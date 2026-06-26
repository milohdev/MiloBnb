import { useEffect, useState, useCallback } from 'react'
import { Home, Calendar, TrendingUp, BarChart2, DollarSign, ChevronDown, ChevronUp } from 'lucide-react'
import api from '@/api/axios'
import type { OwnerDashboardDto, PropertyMetricsDto, ReservationDto } from '@/types'
import LoadingSpinner from '@/components/shared/LoadingSpinner'

function formatCOP(amount: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency', currency: 'COP',
    minimumFractionDigits: 0, maximumFractionDigits: 0,
  }).format(amount)
}

function formatDate(iso: string): string {
  if (!iso) return ''
  const parts = iso.split('-')
  return `${parts[2]}/${parts[1]}/${parts[0]}`
}

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

const STATUS_STYLES: Record<string, string> = {
  Confirmed: 'bg-green-100 text-green-700',
  Cancelled: 'bg-red-100 text-red-700',
  Pending: 'bg-yellow-100 text-yellow-700',
  Completed: 'bg-gray-100 text-gray-600',
}

export default function Dashboard() {
  const [period, setPeriod] = useState<Period>('30d')
  const [customFrom, setCustomFrom] = useState('')
  const [customTo, setCustomTo] = useState('')
  const [dashboard, setDashboard] = useState<OwnerDashboardDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [metrics, setMetrics] = useState<PropertyMetricsDto | null>(null)
  const [metricsLoading, setMetricsLoading] = useState(false)

  const loadDashboard = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const { dateFrom, dateTo } = period === 'custom'
        ? { dateFrom: customFrom, dateTo: customTo }
        : getPeriodDates(period)
      const params = new URLSearchParams()
      if (dateFrom) params.set('dateFrom', dateFrom)
      if (dateTo) params.set('dateTo', dateTo)
      const { data } = await api.get<OwnerDashboardDto>(`/dashboard?${params}`)
      setDashboard(data)
    } catch {
      setError('No se pudo cargar el dashboard.')
    } finally {
      setLoading(false)
    }
  }, [period, customFrom, customTo])

  useEffect(() => {
    if (period !== 'custom') loadDashboard()
  }, [period, loadDashboard])

  const handleCustomSearch = (e: React.FormEvent) => {
    e.preventDefault()
    loadDashboard()
  }

  const handleExpand = async (propertyId: string) => {
    if (expandedId === propertyId) { setExpandedId(null); setMetrics(null); return }
    setExpandedId(propertyId)
    setMetricsLoading(true)
    try {
      const { dateFrom, dateTo } = period === 'custom'
        ? { dateFrom: customFrom, dateTo: customTo }
        : getPeriodDates(period)
      const params = new URLSearchParams()
      if (dateFrom) params.set('dateFrom', dateFrom)
      if (dateTo) params.set('dateTo', dateTo)
      const { data } = await api.get<PropertyMetricsDto>(`/dashboard/properties/${propertyId}?${params}`)
      setMetrics(data)
    } catch {
      setMetrics(null)
    } finally {
      setMetricsLoading(false)
    }
  }

  if (loading) return <LoadingSpinner />

  return (
    <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Dashboard</h1>

      {/* Period selector */}
      <div className="flex flex-wrap items-center gap-3 mb-8">
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

        {period === 'custom' && (
          <form onSubmit={handleCustomSearch} className="flex items-center gap-2 ml-2">
            <input type="date" value={customFrom} onChange={e => setCustomFrom(e.target.value)}
              className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]" />
            <span className="text-gray-400 text-sm">→</span>
            <input type="date" value={customTo} onChange={e => setCustomTo(e.target.value)}
              className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]" />
            <button type="submit"
              className="px-4 py-2 rounded-xl text-white text-sm font-medium transition-colors"
              style={{ backgroundColor: '#2D6A4F' }}
              onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
              onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}>
              Buscar
            </button>
          </form>
        )}
      </div>

      {error && (
        <div className="mb-6 bg-red-50 border border-red-100 text-red-600 rounded-xl px-4 py-3 text-sm">{error}</div>
      )}

      {dashboard && (
        <>
          <p className="text-sm text-gray-500 mb-4">
            Período: <span className="font-medium text-gray-700">{formatDate(dashboard.dateFrom)} — {formatDate(dashboard.dateTo)}</span>
          </p>

          {/* Metric cards */}
          <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
            {[
              { icon: Home, label: 'Propiedades', value: dashboard.totalProperties.toString(), color: 'text-blue-600', bg: 'bg-blue-50' },
              { icon: Calendar, label: 'Reservas', value: dashboard.totalReservations.toString(), color: 'text-purple-600', bg: 'bg-purple-50' },
              { icon: DollarSign, label: 'Ingresos', value: formatCOP(dashboard.totalRevenue), color: 'text-[#2D6A4F]', bg: 'bg-[#f0f7f4]' },
              { icon: BarChart2, label: 'Ocupación', value: `${dashboard.occupancyRate.toFixed(1)}%`, color: 'text-orange-600', bg: 'bg-orange-50' },
              { icon: TrendingUp, label: 'Precio promedio', value: formatCOP(dashboard.averagePricePerNight), color: 'text-indigo-600', bg: 'bg-indigo-50' },
            ].map(({ icon: Icon, label, value, color, bg }) => (
              <div key={label} className="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm">
                <div className={`w-9 h-9 ${bg} rounded-xl flex items-center justify-center mb-3`}>
                  <Icon size={18} className={color} />
                </div>
                <p className="text-xs text-gray-500 mb-1">{label}</p>
                <p className="font-bold text-gray-900 text-sm leading-tight">{value}</p>
              </div>
            ))}
          </div>

          {/* Table */}
          {dashboard.reservationsByProperty.length > 0 ? (
            <div className="bg-white border border-gray-100 rounded-2xl shadow-sm overflow-hidden">
              <div className="px-6 py-4 border-b border-gray-100">
                <h2 className="font-semibold text-gray-900">Reservas por inmueble</h2>
              </div>
              <div className="divide-y divide-gray-50">
                {dashboard.reservationsByProperty.map((row, idx) => (
                  <div key={row.propertyId}>
                    <div
                      className={`px-6 py-4 flex items-center justify-between cursor-pointer hover:bg-gray-50 transition-colors ${idx % 2 === 0 ? '' : 'bg-gray-50/50'}`}
                      onClick={() => handleExpand(row.propertyId)}
                    >
                      <div className="flex-1 min-w-0">
                        <p className="font-medium text-gray-900 truncate">{row.propertyName}</p>
                      </div>
                      <div className="flex items-center gap-8 text-sm text-gray-600 shrink-0 ml-4">
                        <span>{row.reservationCount} reservas</span>
                        <span className="font-semibold text-gray-900">{formatCOP(row.revenue)}</span>
                        <button className="text-[#2D6A4F] flex items-center gap-1 text-xs font-medium">
                          {expandedId === row.propertyId ? <><ChevronUp size={14} />Ocultar</> : <><ChevronDown size={14} />Ver detalle</>}
                        </button>
                      </div>
                    </div>

                    {expandedId === row.propertyId && (
                      <div className="px-6 pb-4 bg-[#f8fdf9] border-t border-[#2D6A4F]/10">
                        {metricsLoading ? (
                          <div className="py-4 flex justify-center"><div className="w-5 h-5 border-2 border-[#2D6A4F] border-t-transparent rounded-full animate-spin" /></div>
                        ) : metrics ? (
                          <div className="pt-4 space-y-4">
                            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                              {[
                                { label: 'Ocupación', value: `${metrics.occupancyRate.toFixed(1)}%` },
                                { label: 'Ingresos', value: formatCOP(metrics.totalRevenue) },
                                { label: 'Promedio noches', value: metrics.averageLengthOfStay.toFixed(1) },
                                { label: 'Total reservas', value: metrics.totalReservations.toString() },
                              ].map(({ label, value }) => (
                                <div key={label} className="bg-white rounded-xl p-3 border border-gray-100">
                                  <p className="text-xs text-gray-500">{label}</p>
                                  <p className="font-semibold text-gray-900 mt-0.5">{value}</p>
                                </div>
                              ))}
                            </div>
                            {metrics.recentReservations.length > 0 && (
                              <div>
                                <p className="text-sm font-medium text-gray-700 mb-2">Últimas reservas</p>
                                <div className="overflow-x-auto">
                                  <table className="w-full text-sm">
                                    <thead>
                                      <tr className="text-left text-xs text-gray-500">
                                        <th className="pb-2 font-medium">Huésped</th>
                                        <th className="pb-2 font-medium">Check-in</th>
                                        <th className="pb-2 font-medium">Check-out</th>
                                        <th className="pb-2 font-medium">Noches</th>
                                        <th className="pb-2 font-medium">Total</th>
                                        <th className="pb-2 font-medium">Estado</th>
                                      </tr>
                                    </thead>
                                    <tbody className="divide-y divide-gray-100">
                                      {metrics.recentReservations.slice(0, 5).map((r: ReservationDto) => (
                                        <tr key={r.id} className="text-gray-700">
                                          <td className="py-2">{r.guestFullName}</td>
                                          <td className="py-2">{formatDate(r.checkInDate)}</td>
                                          <td className="py-2">{formatDate(r.checkOutDate)}</td>
                                          <td className="py-2">{r.totalNights}</td>
                                          <td className="py-2 font-medium">{formatCOP(r.totalPrice)}</td>
                                          <td className="py-2">
                                            <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${STATUS_STYLES[r.status] ?? 'bg-gray-100 text-gray-600'}`}>
                                              {r.status}
                                            </span>
                                          </td>
                                        </tr>
                                      ))}
                                    </tbody>
                                  </table>
                                </div>
                              </div>
                            )}
                          </div>
                        ) : (
                          <p className="text-gray-500 text-sm py-4">No se pudo cargar el detalle.</p>
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="text-center py-16 bg-white border border-gray-100 rounded-2xl">
              <BarChart2 size={40} className="mx-auto text-gray-200 mb-3" />
              <p className="text-gray-500">No hay datos para el período seleccionado</p>
            </div>
          )}
        </>
      )}
    </div>
  )
}
