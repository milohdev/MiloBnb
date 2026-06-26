import { useEffect, useState } from 'react'
import { Bell } from 'lucide-react'
import api from '@/api/axios'
import type { NotificationDto, PagedResult } from '@/types'
import LoadingSpinner from '@/components/shared/LoadingSpinner'

const TYPE_LABELS: Record<string, { label: string; className: string }> = {
  ReservationConfirmed: { label: 'Reserva', className: 'bg-green-100 text-green-700' },
  ReservationCancelled: { label: 'Cancelación', className: 'bg-red-100 text-red-700' },
  KycApproved: { label: 'KYC aprobado', className: 'bg-blue-100 text-blue-700' },
  KycRejected: { label: 'KYC rechazado', className: 'bg-orange-100 text-orange-700' },
  CheckInReminder: { label: 'Check-in', className: 'bg-purple-100 text-purple-700' },
  CheckOutReminder: { label: 'Check-out', className: 'bg-purple-100 text-purple-700' },
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('es-CO', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

export default function Notifications() {
  const [result, setResult] = useState<PagedResult<NotificationDto> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    api.get<PagedResult<NotificationDto>>('/notifications?page=1&pageSize=20')
      .then(r => setResult(r.data))
      .catch(() => setError('No se pudieron cargar las notificaciones.'))
      .finally(() => setLoading(false))
  }, [])

  const handleRead = async (id: string) => {
    if (!result) return
    const notif = result.items.find(n => n.id === id)
    if (!notif || notif.isRead) return
    try {
      await api.put(`/notifications/${id}/read`)
      setResult(prev => prev ? {
        ...prev,
        items: prev.items.map(n => n.id === id ? { ...n, isRead: true } : n)
      } : prev)
    } catch {
      // silent
    }
  }

  if (loading) return <LoadingSpinner />

  const items = result?.items ?? []
  const unreadCount = items.filter(n => !n.isRead).length

  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <div className="flex items-center gap-3 mb-6">
        <Bell size={22} className="text-[#2D6A4F]" />
        <h1 className="text-2xl font-bold text-gray-900">Notificaciones</h1>
        {unreadCount > 0 && (
          <span className="bg-[#2D6A4F] text-white text-xs font-bold px-2 py-0.5 rounded-full">
            {unreadCount}
          </span>
        )}
      </div>

      {error && (
        <div className="mb-6 bg-red-50 border border-red-100 text-red-600 rounded-xl px-4 py-3 text-sm">
          {error}
        </div>
      )}

      {items.length === 0 ? (
        <div className="text-center py-20">
          <Bell size={48} className="mx-auto text-gray-200 mb-4" />
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Sin notificaciones</h3>
          <p className="text-gray-500">Aquí aparecerán las actualizaciones sobre tus reservas y cuenta</p>
        </div>
      ) : (
        <div className="space-y-2">
          {items.map(n => {
            const typeInfo = TYPE_LABELS[n.type] ?? { label: n.type, className: 'bg-gray-100 text-gray-600' }
            return (
              <div
                key={n.id}
                onClick={() => handleRead(n.id)}
                className={`rounded-2xl p-4 border cursor-pointer transition-colors ${
                  n.isRead
                    ? 'bg-white border-gray-100 hover:bg-gray-50'
                    : 'bg-[#f0f7f4] border-[#2D6A4F]/20 hover:bg-[#e8f3ee]'
                }`}
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap mb-1">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${typeInfo.className}`}>
                      {typeInfo.label}
                    </span>
                    {!n.isRead && (
                      <span className="w-2 h-2 rounded-full bg-[#2D6A4F] shrink-0" />
                    )}
                  </div>
                  <p className="font-semibold text-gray-900 text-sm">{n.title}</p>
                  <p className="text-gray-600 text-sm mt-0.5">{n.body}</p>
                  <p className="text-gray-400 text-xs mt-2">{formatDateTime(n.createdAt)}</p>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
