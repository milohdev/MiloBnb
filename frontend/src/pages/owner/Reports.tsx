import { useState } from 'react'
import { FileSpreadsheet, Download, AlertCircle } from 'lucide-react'
import api from '@/api/axios'

type Period = '30d' | '3m' | '6m' | '1y' | 'custom'

function getPeriodDates(period: Period): { dateFrom: string; dateTo: string } {
  const today = new Date()
  const fmt = (d: Date) => d.toISOString().split('T')[0]
  const from = new Date(today)
  switch (period) {
    case '30d': from.setDate(today.getDate() - 30); break
    case '3m': from.setMonth(today.getMonth() - 3); break
    case '6m': from.setMonth(today.getMonth() - 6); break
    case '1y': from.setFullYear(today.getFullYear() - 1); break
    default: return { dateFrom: '', dateTo: '' }
  }
  return { dateFrom: fmt(from), dateTo: fmt(today) }
}

interface ReportOption {
  id: string
  title: string
  description: string
  endpoint: string
  filename: string
}

const REPORTS: ReportOption[] = [
  {
    id: 'reservations',
    title: 'Reporte de Reservas',
    description: 'Listado completo de todas las reservas: fechas, huéspedes, estados y montos.',
    endpoint: '/reports/reservations',
    filename: 'reservas',
  },
]

export default function Reports() {
  const [period, setPeriod] = useState<Period>('30d')
  const [customFrom, setCustomFrom] = useState('')
  const [customTo, setCustomTo] = useState('')
  const [downloading, setDownloading] = useState<string | null>(null)
  const [error, setError] = useState('')

  const handleDownload = async (report: ReportOption) => {
    setError('')
    setDownloading(report.id)

    const { dateFrom, dateTo } =
      period === 'custom' ? { dateFrom: customFrom, dateTo: customTo } : getPeriodDates(period)

    if (period === 'custom' && (!dateFrom || !dateTo)) {
      setError('Selecciona un rango de fechas personalizado.')
      setDownloading(null)
      return
    }

    try {
      const params = new URLSearchParams()
      if (dateFrom) params.set('dateFrom', dateFrom)
      if (dateTo) params.set('dateTo', dateTo)

      const response = await api.get(`${report.endpoint}?${params}`, {
        responseType: 'blob',
      })

      const url = URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = `${report.filename}_${dateFrom ?? 'all'}_${dateTo ?? 'all'}.xlsx`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(url)
    } catch {
      setError(`No se pudo descargar el reporte de ${report.title.toLowerCase()}.`)
    } finally {
      setDownloading(null)
    }
  }

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <div className="flex items-center gap-3 mb-2">
        <FileSpreadsheet size={24} className="text-[#2D6A4F]" />
        <h1 className="text-2xl font-bold text-gray-900">Reportes</h1>
      </div>
      <p className="text-gray-500 text-sm mb-8">Descarga reportes en formato Excel para el período que elijas.</p>

      {/* Period selector */}
      <div className="mb-8">
        <h2 className="text-sm font-medium text-gray-700 mb-3">Período del reporte</h2>
        <div className="flex flex-wrap items-center gap-3">
          {([['30d', 'Últimos 30 días'], ['3m', 'Últimos 3 meses'], ['6m', 'Últimos 6 meses'], ['1y', 'Este año'], ['custom', 'Personalizado']] as [Period, string][]).map(([key, label]) => (
            <button
              key={key}
              onClick={() => setPeriod(key)}
              className={`px-4 py-2 rounded-xl text-sm font-medium transition-colors border ${
                period === key
                  ? 'border-[#2D6A4F] bg-[#2D6A4F] text-white'
                  : 'border-gray-200 text-gray-600 hover:border-[#2D6A4F] hover:text-[#2D6A4F]'
              }`}
            >
              {label}
            </button>
          ))}
        </div>

        {period === 'custom' && (
          <div className="mt-4 flex flex-wrap items-center gap-3">
            <div>
              <label className="block text-xs text-gray-500 mb-1">Desde</label>
              <input
                type="date"
                value={customFrom}
                onChange={e => setCustomFrom(e.target.value)}
                className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
              />
            </div>
            <span className="text-gray-400 text-sm mt-4">→</span>
            <div>
              <label className="block text-xs text-gray-500 mb-1">Hasta</label>
              <input
                type="date"
                value={customTo}
                onChange={e => setCustomTo(e.target.value)}
                className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
              />
            </div>
          </div>
        )}
      </div>

      {error && (
        <div className="mb-6 flex items-center gap-2 bg-red-50 border border-red-100 text-red-600 rounded-xl px-4 py-3 text-sm">
          <AlertCircle size={16} className="shrink-0" />
          {error}
        </div>
      )}

      {/* Report cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
        {REPORTS.map(report => (
          <div
            key={report.id}
            className="bg-white border border-gray-100 rounded-2xl p-6 shadow-sm flex flex-col"
          >
            <div className="w-10 h-10 bg-[#f0f7f4] rounded-xl flex items-center justify-center mb-4">
              <FileSpreadsheet size={20} className="text-[#2D6A4F]" />
            </div>
            <h3 className="font-semibold text-gray-900 mb-1">{report.title}</h3>
            <p className="text-sm text-gray-500 flex-1 mb-5">{report.description}</p>
            <button
              onClick={() => handleDownload(report)}
              disabled={downloading === report.id}
              className="flex items-center justify-center gap-2 py-2.5 rounded-xl text-white text-sm font-medium transition-colors disabled:opacity-60"
              style={{ backgroundColor: '#2D6A4F' }}
              onMouseEnter={e => downloading !== report.id && (e.currentTarget.style.backgroundColor = '#265c44')}
              onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
            >
              {downloading === report.id ? (
                <div className="flex items-center gap-2">
                  <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  Descargando...
                </div>
              ) : (
                <>
                  <Download size={16} />
                  Descargar Excel
                </>
              )}
            </button>
          </div>
        ))}
      </div>
    </div>
  )
}
