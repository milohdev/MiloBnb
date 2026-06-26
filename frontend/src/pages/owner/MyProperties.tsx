import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Plus, Edit, Trash2, MapPin, Home } from 'lucide-react'
import api from '@/api/axios'
import type { PropertyDto } from '@/types'
import LoadingSpinner from '@/components/shared/LoadingSpinner'

function formatCOP(amount: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency', currency: 'COP',
    minimumFractionDigits: 0, maximumFractionDigits: 0,
  }).format(amount)
}

export default function MyProperties() {
  const navigate = useNavigate()
  const [properties, setProperties] = useState<PropertyDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [confirmId, setConfirmId] = useState<string | null>(null)

  useEffect(() => {
    api.get<PropertyDto[]>('/properties/mine')
      .then(r => setProperties(r.data))
      .catch(() => setError('No se pudieron cargar tus propiedades.'))
      .finally(() => setLoading(false))
  }, [])

  const handleDelete = async (id: string) => {
    setConfirmId(null)
    setDeletingId(id)
    try {
      await api.delete(`/properties/${id}`)
      setProperties(prev => prev.filter(p => p.id !== id))
    } catch {
      setError('No se pudo eliminar la propiedad.')
    } finally {
      setDeletingId(null)
    }
  }

  if (loading) return <LoadingSpinner />

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Mis Inmuebles</h1>
        <Link
          to="/my-properties/create"
          className="flex items-center gap-2 px-4 py-2 rounded-xl text-white text-sm font-medium transition-colors"
          style={{ backgroundColor: '#2D6A4F' }}
          onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
          onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
        >
          <Plus size={16} />
          Nueva propiedad
        </Link>
      </div>

      {error && (
        <div className="mb-6 bg-red-50 border border-red-100 text-red-600 rounded-xl px-4 py-3 text-sm">{error}</div>
      )}

      {properties.length === 0 ? (
        <div className="text-center py-20">
          <Home size={48} className="mx-auto text-gray-200 mb-4" />
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Aún no tienes inmuebles publicados</h3>
          <p className="text-gray-500 mb-6">Publica tu primer inmueble y empieza a recibir reservas</p>
          <Link
            to="/my-properties/create"
            className="inline-block px-6 py-2.5 rounded-xl text-white text-sm font-medium transition-colors"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
          >
            Publicar mi primer inmueble
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {properties.map(p => (
            <div key={p.id} className="bg-white border border-gray-100 rounded-2xl overflow-hidden shadow-sm">
              <div className="aspect-[4/3] bg-gray-100 relative">
                {p.images[0] ? (
                  <img src={p.images[0]} alt={p.name} className="w-full h-full object-cover" />
                ) : (
                  <div className="w-full h-full flex items-center justify-center text-5xl text-gray-200">🏠</div>
                )}
                <div className="absolute top-3 left-3 flex gap-2 flex-wrap">
                  <span className={`text-xs font-semibold px-2 py-1 rounded-full ${p.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                    {p.isActive ? 'Activo' : 'Inactivo'}
                  </span>
                  {p.allowSameDayBooking && (
                    <span className="text-xs font-semibold px-2 py-1 rounded-full bg-white text-[#2D6A4F] border border-[#2D6A4F]/30">
                      Reserva inmediata
                    </span>
                  )}
                </div>
              </div>

              <div className="p-4">
                <h3 className="font-semibold text-gray-900 truncate">{p.name}</h3>
                <div className="flex items-center gap-1 text-gray-500 text-sm mt-1">
                  <MapPin size={13} />
                  <span>{p.city}, {p.country}</span>
                </div>
                <div className="mt-2 flex items-baseline gap-1">
                  <span className="font-bold text-gray-900">{formatCOP(p.pricePerNight)}</span>
                  <span className="text-gray-400 text-xs">/ noche</span>
                </div>

                <div className="mt-4 flex items-center gap-2">
                  <button
                    onClick={() => navigate(`/my-properties/${p.id}/edit`)}
                    className="flex-1 flex items-center justify-center gap-1.5 py-2 rounded-lg border border-[#2D6A4F] text-[#2D6A4F] text-sm font-medium hover:bg-[#f0f7f4] transition-colors"
                  >
                    <Edit size={14} />
                    Editar
                  </button>

                  {confirmId === p.id ? (
                    <div className="flex-1 flex items-center gap-1.5">
                      <button
                        onClick={() => handleDelete(p.id)}
                        disabled={deletingId === p.id}
                        className="flex-1 py-2 rounded-lg bg-red-500 text-white text-xs font-medium hover:bg-red-600 transition-colors disabled:opacity-50"
                      >
                        Confirmar
                      </button>
                      <button
                        onClick={() => setConfirmId(null)}
                        className="flex-1 py-2 rounded-lg bg-gray-100 text-gray-600 text-xs font-medium hover:bg-gray-200 transition-colors"
                      >
                        Cancelar
                      </button>
                    </div>
                  ) : (
                    <button
                      onClick={() => setConfirmId(p.id)}
                      disabled={deletingId === p.id}
                      className="flex items-center gap-1.5 py-2 px-3 rounded-lg text-red-500 hover:bg-red-50 transition-colors text-sm"
                    >
                      <Trash2 size={14} />
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
