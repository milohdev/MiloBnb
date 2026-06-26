import { Heart, MapPin, Users, BedDouble, Bath } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import type { PropertyDto } from '@/types'
import { useAuth } from '@/hooks/useAuth'

interface Props {
  property: PropertyDto
  isInWishlist?: boolean
  onWishlistToggle?: (propertyId: string, inWishlist: boolean) => void
}

function formatCOP(amount: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

export default function PropertyCard({ property, isInWishlist = false, onWishlistToggle }: Props) {
  const navigate = useNavigate()
  const { isGuest } = useAuth()

  return (
    <div className="rounded-xl overflow-hidden border border-gray-100 bg-white hover:shadow-lg transition-shadow duration-200 flex flex-col">
      {/* Image */}
      <div className="relative aspect-[4/3] bg-gray-100">
        {property.images[0] ? (
          <img
            src={property.images[0]}
            alt={property.name}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-5xl text-gray-200">
            🏠
          </div>
        )}

        {/* Badges */}
        <div className="absolute top-3 left-3 flex gap-2">
          {property.allowSameDayBooking && (
            <span className="bg-white text-[#2D6A4F] text-xs font-semibold px-2.5 py-1 rounded-full shadow-sm border border-[#2D6A4F]/20">
              Reserva inmediata
            </span>
          )}
        </div>

        {/* Wishlist button */}
        {isGuest && (
          <button
            onClick={(e) => {
              e.stopPropagation()
              onWishlistToggle?.(property.id, isInWishlist)
            }}
            className="absolute top-3 right-3 p-2 rounded-full bg-white shadow-sm hover:scale-110 transition-transform"
            aria-label={isInWishlist ? 'Quitar de favoritos' : 'Agregar a favoritos'}
          >
            <Heart
              size={18}
              className={isInWishlist ? 'text-red-500 fill-red-500' : 'text-gray-400'}
            />
          </button>
        )}
      </div>

      {/* Content */}
      <div className="p-4 flex flex-col flex-1">
        <h3 className="font-semibold text-gray-900 truncate">{property.name}</h3>

        <div className="flex items-center gap-1 text-gray-500 text-sm mt-1">
          <MapPin size={13} />
          <span className="truncate">{property.city}, {property.country}</span>
        </div>

        <div className="flex items-center gap-3 text-gray-400 text-xs mt-2">
          <span className="flex items-center gap-1"><Users size={12} />{property.maxGuests} huéspedes</span>
          <span className="flex items-center gap-1"><BedDouble size={12} />{property.bedrooms} hab.</span>
          <span className="flex items-center gap-1"><Bath size={12} />{property.bathrooms} baños</span>
        </div>

        <div className="mt-auto pt-3 flex items-center justify-between">
          <div className="flex items-baseline gap-1">
            <span className="font-bold text-gray-900 text-base">{formatCOP(property.pricePerNight)}</span>
            <span className="text-gray-400 text-xs">/ noche</span>
          </div>
          <button
            onClick={() => navigate(`/properties/${property.id}`)}
            className="text-sm font-medium px-3 py-1.5 rounded-lg text-white transition-colors"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
          >
            Ver detalle
          </button>
        </div>
      </div>
    </div>
  )
}
