import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { MapPin, Users, BedDouble, Bath, Clock, Heart, ChevronLeft, ChevronRight } from 'lucide-react'
import api from '@/api/axios'
import type { CreateReservationRequest, CurrentUserDto, PropertyDto, ReservationDto } from '@/types'
import LoadingSpinner from '@/components/shared/LoadingSpinner'
import { useAuth } from '@/hooks/useAuth'

function formatCOP(amount: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

function diffDays(from: string, to: string): number {
  const a = new Date(from), b = new Date(to)
  return Math.max(0, Math.round((b.getTime() - a.getTime()) / 86400000))
}

function formatDate(iso: string): string {
  if (!iso) return ''
  const [y, m, d] = iso.split('-')
  return `${d}/${m}/${y}`
}

export default function PropertyDetail() {
  const { id } = useParams<{ id: string }>()
  const { isGuest, isAuthenticated } = useAuth()

  const [property, setProperty] = useState<PropertyDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  const [inWishlist, setInWishlist] = useState(false)
  const [wishlistLoading, setWishlistLoading] = useState(false)

  const [isKycVerified, setIsKycVerified] = useState(false)
  const [kycLoaded, setKycLoaded] = useState(false)

  const [imgIndex, setImgIndex] = useState(0)

  const [checkIn, setCheckIn] = useState('')
  const [checkOut, setCheckOut] = useState('')
  const [reserving, setReserving] = useState(false)
  const [reserveSuccess, setReserveSuccess] = useState<ReservationDto | null>(null)
  const [reserveError, setReserveError] = useState('')

  useEffect(() => {
    if (!id) return
    api.get<PropertyDto>(`/properties/${id}`)
      .then(r => setProperty(r.data))
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }, [id])

  useEffect(() => {
    if (!isGuest) return
    api.get<CurrentUserDto>('/auth/me')
      .then(r => {
        setIsKycVerified(r.data.isKycVerified)
        setKycLoaded(true)
      })
      .catch(() => setKycLoaded(true))
    if (id) {
      api.get<PropertyDto[]>('/wishlist')
        .then(r => setInWishlist(r.data.some(p => p.id === id)))
        .catch(() => { /* silent */ })
    }
  }, [isGuest, id])

  const toggleWishlist = async () => {
    if (!id) return
    setWishlistLoading(true)
    try {
      if (inWishlist) {
        await api.delete(`/wishlist/${id}`)
        setInWishlist(false)
      } else {
        await api.post(`/wishlist/${id}`)
        setInWishlist(true)
      }
    } catch {
      // silent
    } finally {
      setWishlistLoading(false)
    }
  }

  const nights = diffDays(checkIn, checkOut)
  const totalPrice = property ? nights * property.pricePerNight : 0

  const handleReserve = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!id || !property) return
    setReserveError('')
    setReserving(true)
    try {
      const body: CreateReservationRequest = {
        propertyId: id,
        checkInDate: checkIn,
        checkOutDate: checkOut,
      }
      const { data } = await api.post<ReservationDto>('/reservations', body)
      setReserveSuccess(data)
    } catch (err: unknown) {
      const e = err as { response?: { status?: number; data?: { title?: string } } }
      const status = e?.response?.status
      const msg = e?.response?.data?.title ?? ''
      if (status === 403 && msg.includes('identidad')) {
        setReserveError('kyc')
      } else if (status === 409) {
        setReserveError('Las fechas seleccionadas ya no están disponibles. Por favor elige otras fechas.')
      } else if (msg.includes('mismo día')) {
        setReserveError('Este inmueble no permite reservas para el mismo día. La fecha mínima de check-in es mañana.')
      } else {
        setReserveError(msg || 'No se pudo completar la reserva. Intenta de nuevo.')
      }
    } finally {
      setReserving(false)
    }
  }

  if (loading) return <LoadingSpinner />

  if (notFound || !property) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-24 text-center">
        <h1 className="text-2xl font-bold text-gray-900">Propiedad no encontrada</h1>
        <Link to="/" className="mt-4 inline-block text-[#2D6A4F] hover:underline">← Volver al catálogo</Link>
      </div>
    )
  }

  const images = property.images

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <Link to="/" className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-[#2D6A4F] mb-6 transition-colors">
        <ChevronLeft size={16} />
        Volver al catálogo
      </Link>

      <div className="flex items-start justify-between gap-4 mb-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">{property.name}</h1>
          <div className="flex items-center gap-1 text-gray-500 text-sm mt-1">
            <MapPin size={14} />
            {property.city}, {property.country}
          </div>
        </div>
        {isGuest && (
          <button
            onClick={toggleWishlist}
            disabled={wishlistLoading}
            className="flex items-center gap-2 px-4 py-2 rounded-xl border border-gray-200 text-sm font-medium text-gray-700 hover:border-[#2D6A4F] transition-colors"
          >
            <Heart
              size={16}
              className={inWishlist ? 'text-red-500 fill-red-500' : 'text-gray-400'}
            />
            {inWishlist ? 'En favoritos' : 'Guardar'}
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-6">
          {/* Gallery */}
          <div className="relative rounded-2xl overflow-hidden bg-gray-100 aspect-[16/9]">
            {images.length > 0 ? (
              <>
                <img
                  src={images[imgIndex]}
                  alt={`${property.name} - imagen ${imgIndex + 1}`}
                  className="w-full h-full object-cover"
                />
                {images.length > 1 && (
                  <>
                    <button
                      onClick={() => setImgIndex(i => (i - 1 + images.length) % images.length)}
                      className="absolute left-3 top-1/2 -translate-y-1/2 bg-white/90 hover:bg-white rounded-full p-2 shadow transition"
                    >
                      <ChevronLeft size={18} />
                    </button>
                    <button
                      onClick={() => setImgIndex(i => (i + 1) % images.length)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 bg-white/90 hover:bg-white rounded-full p-2 shadow transition"
                    >
                      <ChevronRight size={18} />
                    </button>
                    <div className="absolute bottom-3 left-1/2 -translate-x-1/2 bg-black/50 text-white text-xs px-2.5 py-1 rounded-full">
                      {imgIndex + 1} / {images.length}
                    </div>
                  </>
                )}
              </>
            ) : (
              <div className="w-full h-full flex items-center justify-center text-6xl text-gray-200">🏠</div>
            )}
          </div>

          {/* Badges & stats */}
          <div className="flex flex-wrap items-center gap-3">
            {property.allowSameDayBooking && (
              <span className="bg-[#2D6A4F]/10 text-[#2D6A4F] text-xs font-semibold px-3 py-1.5 rounded-full">
                ✓ Reserva inmediata
              </span>
            )}
            <span className="flex items-center gap-1.5 text-sm text-gray-600 bg-gray-100 px-3 py-1.5 rounded-full">
              <Users size={14} /> {property.maxGuests} huéspedes
            </span>
            <span className="flex items-center gap-1.5 text-sm text-gray-600 bg-gray-100 px-3 py-1.5 rounded-full">
              <BedDouble size={14} /> {property.bedrooms} habitaciones
            </span>
            <span className="flex items-center gap-1.5 text-sm text-gray-600 bg-gray-100 px-3 py-1.5 rounded-full">
              <Bath size={14} /> {property.bathrooms} baños
            </span>
          </div>

          <div>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">Descripción</h2>
            <p className="text-gray-600 leading-relaxed whitespace-pre-line">{property.description}</p>
          </div>

          <div>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">Ubicación</h2>
            <p className="text-gray-600">{property.address}, {property.city}, {property.country}</p>
          </div>

          <div className="border border-gray-100 rounded-xl p-4 space-y-2">
            <h2 className="text-lg font-semibold text-gray-900 mb-3">Políticas</h2>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Clock size={15} className="text-[#2D6A4F]" />
              <span>Check-in: a partir de las 14:00</span>
            </div>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Clock size={15} className="text-[#2D6A4F]" />
              <span>Check-out: hasta las 12:00</span>
            </div>
          </div>
        </div>

        {/* Reservation card */}
        <div className="lg:col-span-1">
          <div className="sticky top-24 border border-gray-200 rounded-2xl p-5 shadow-sm bg-white">
            <div className="flex items-baseline gap-1 mb-5">
              <span className="text-2xl font-bold text-gray-900">{formatCOP(property.pricePerNight)}</span>
              <span className="text-gray-500 text-sm">/ noche</span>
            </div>

            {reserveSuccess ? (
              <div className="bg-green-50 border border-green-200 rounded-xl p-4 text-center">
                <div className="text-2xl mb-2">✓</div>
                <p className="font-semibold text-green-800">¡Reserva confirmada!</p>
                <p className="text-green-700 text-sm mt-1">
                  {formatDate(reserveSuccess.checkInDate)} → {formatDate(reserveSuccess.checkOutDate)}
                </p>
                <p className="text-green-700 text-sm">{reserveSuccess.totalNights} noches · {formatCOP(reserveSuccess.totalPrice)}</p>
              </div>
            ) : !isAuthenticated ? (
              <div className="text-center">
                <p className="text-gray-600 text-sm mb-3">Inicia sesión para hacer una reserva</p>
                <Link
                  to="/login"
                  className="block w-full py-2.5 rounded-xl text-white text-sm font-medium text-center transition-colors"
                  style={{ backgroundColor: '#2D6A4F' }}
                  onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
                  onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
                >
                  Iniciar sesión para reservar
                </Link>
              </div>
            ) : !isGuest ? (
              <p className="text-sm text-gray-500 text-center">Solo los huéspedes pueden hacer reservas.</p>
            ) : kycLoaded && !isKycVerified ? (
              <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 text-center">
                <p className="text-amber-800 text-sm font-medium mb-2">Verifica tu identidad para reservar</p>
                <Link to="/kyc" className="text-[#2D6A4F] text-sm font-semibold hover:underline">
                  Completar verificación →
                </Link>
              </div>
            ) : (
              <form onSubmit={handleReserve} className="space-y-3">
                <div>
                  <label className="block text-xs font-medium text-gray-500 mb-1">Check-in</label>
                  <input
                    type="date"
                    required
                    value={checkIn}
                    onChange={e => { setCheckIn(e.target.value); setReserveError('') }}
                    className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-500 mb-1">Check-out</label>
                  <input
                    type="date"
                    required
                    value={checkOut}
                    min={checkIn}
                    onChange={e => { setCheckOut(e.target.value); setReserveError('') }}
                    className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
                  />
                </div>

                {nights > 0 && (
                  <div className="bg-gray-50 rounded-xl p-3 text-sm space-y-1">
                    <div className="flex justify-between text-gray-600">
                      <span>{formatCOP(property.pricePerNight)} × {nights} noches</span>
                      <span>{formatCOP(totalPrice)}</span>
                    </div>
                    <div className="flex justify-between font-semibold text-gray-900 border-t border-gray-200 pt-1 mt-1">
                      <span>Total</span>
                      <span>{formatCOP(totalPrice)}</span>
                    </div>
                  </div>
                )}

                {reserveError && reserveError !== 'kyc' && (
                  <div className="bg-red-50 border border-red-100 rounded-lg p-3 text-red-600 text-xs">
                    {reserveError}
                  </div>
                )}
                {reserveError === 'kyc' && (
                  <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 text-amber-800 text-xs">
                    Necesitas verificar tu identidad.{' '}
                    <Link to="/kyc" className="font-semibold underline">Ir a verificación</Link>
                  </div>
                )}

                <button
                  type="submit"
                  disabled={reserving || nights === 0}
                  className="w-full py-2.5 rounded-xl text-white text-sm font-medium transition-colors disabled:opacity-50"
                  style={{ backgroundColor: '#2D6A4F' }}
                  onMouseEnter={e => !reserving && (e.currentTarget.style.backgroundColor = '#265c44')}
                  onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
                >
                  {reserving ? 'Reservando...' : 'Reservar ahora'}
                </button>
              </form>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
