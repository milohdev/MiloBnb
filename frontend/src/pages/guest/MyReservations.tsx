import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Calendar, Clock, XCircle } from 'lucide-react'
import api from '@/api/axios'
import type { ReservationDto } from '@/types'
import LoadingSpinner from '@/components/shared/LoadingSpinner'

function formatCOP(amount: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

function formatDate(iso: string): string {
  if (!iso) return ''
  const [, m, d] = iso.split('-')
  const y = iso.split('-')[0]
  return `${d}/${m}/${y}`
}

const STATUS_STYLES: Record<string, { label: string; className: string }> = {
  Confirmed: { label: 'Confirmada', className: 'bg-green-100 text-green-800' },
  Cancelled: { label: 'Cancelada', className: 'bg-red-100 text-red-800' },
  Pending: { label: 'Pendiente', className: 'bg-yellow-100 text-yellow-800' },
  Completed: { label: 'Completada', className: 'bg-gray-100 text-gray-700' },
}

export default function MyReservations() {
  const [reservations, setReservations] = useState<ReservationDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [cancelling, setCancelling] = useState<string | null>(null)
  const [confirmId, setConfirmId] = useState<string | null>(null)

  useEffect(() => {
    api.get<ReservationDto[]>('/reservations/my')
      .then(r => setReservations(r.data))
      .catch(() => setError('No se pudieron cargar las reservas.'))
      .finally(() => setLoading(false))
  }, [])

  const handleCancel = async (id: string) => {
    setConfirmId(null)
    setCancelling(id)
    try {
      await api.delete(`/reservations/${id}`)
      setReservations(prev =>
        prev.map(r => r.id === id ? { ...r, status: 'Cancelled' } : r)
      )
    } catch {
      setError('No se pudo cancelar la reserva.')
    } finally {
      setCancelling(null)
    }
  }

  if (loading) return <LoadingSpinner />

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Mis Reservas</h1>

      {error && (
        <div className="mb-6 bg-red-50 border border-red-100 text-red-600 rounded-xl px-4 py-3 text-sm">
          {error}
        </div>
      )}

      {reservations.length === 0 ? (
        <div className="text-center py-20">
          <div className="text-5xl mb-4">🏡</div>
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Aún no tienes reservas</h3>
          <p className="text-gray-500 mb-6">Explora el catálogo y encuentra tu próximo alojamiento</p>
          <Link
            to="/"
            className="inline-block px-6 py-2.5 rounded-xl text-white text-sm font-medium transition-colors"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
          >
            Ver catálogo
          </Link>
        </div>
      ) : (
        <div className="space-y-4">
          {reservations.map(r => {
            const statusInfo = STATUS_STYLES[r.status] ?? { label: r.status, className: 'bg-gray-100 text-gray-700' }
            return (
              <div key={r.id} className="bg-white border border-gray-100 rounded-2xl p-5 shadow-sm">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-3 flex-wrap">
                      <h3 className="font-semibold text-gray-900 truncate">{r.propertyName}</h3>
                      <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${statusInfo.className}`}>
                        {statusInfo.label}
                      </span>
                    </div>

                    <div className="mt-3 grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm text-gray-600">
                      <div className="flex items-center gap-2">
                        <Calendar size={15} className="text-[#2D6A4F] shrink-0" />
                        <span>
                          <span className="font-medium">Check-in:</span> {formatDate(r.checkInDate)} a las 14:00
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        <Calendar size={15} className="text-[#2D6A4F] shrink-0" />
                        <span>
                          <span className="font-medium">Check-out:</span> {formatDate(r.checkOutDate)} a las 12:00
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        <Clock size={15} className="text-[#2D6A4F] shrink-0" />
                        <span>{r.totalNights} {r.totalNights === 1 ? 'noche' : 'noches'}</span>
                      </div>
                      <div className="font-semibold text-gray-900">
                        Total: {formatCOP(r.totalPrice)}
                      </div>
                    </div>
                  </div>

                  {r.status === 'Confirmed' && (
                    <div>
                      {confirmId === r.id ? (
                        <div className="flex items-center gap-2">
                          <span className="text-xs text-gray-500">¿Estás seguro?</span>
                          <button
                            onClick={() => handleCancel(r.id)}
                            disabled={cancelling === r.id}
                            className="text-xs px-3 py-1.5 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors disabled:opacity-50"
                          >
                            Sí, cancelar
                          </button>
                          <button
                            onClick={() => setConfirmId(null)}
                            className="text-xs px-3 py-1.5 bg-gray-100 text-gray-600 rounded-lg hover:bg-gray-200 transition-colors"
                          >
                            No
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => setConfirmId(r.id)}
                          disabled={cancelling === r.id}
                          className="flex items-center gap-1.5 text-sm text-red-500 hover:text-red-700 transition-colors"
                        >
                          <XCircle size={16} />
                          Cancelar
                        </button>
                      )}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
