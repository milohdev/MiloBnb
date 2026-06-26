import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Heart } from 'lucide-react'
import api from '@/api/axios'
import type { PropertyDto } from '@/types'
import PropertyCard from '@/components/shared/PropertyCard'
import LoadingSpinner from '@/components/shared/LoadingSpinner'

export default function Wishlist() {
  const [properties, setProperties] = useState<PropertyDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    api.get<PropertyDto[]>('/wishlist')
      .then(r => setProperties(r.data))
      .catch(() => setError('No se pudo cargar tu lista de favoritos.'))
      .finally(() => setLoading(false))
  }, [])

  const handleRemove = async (propertyId: string) => {
    try {
      await api.delete(`/wishlist/${propertyId}`)
      setProperties(prev => prev.filter(p => p.id !== propertyId))
    } catch {
      // silent
    }
  }

  if (loading) return <LoadingSpinner />

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <div className="flex items-center gap-3 mb-6">
        <Heart size={22} className="text-[#2D6A4F]" />
        <h1 className="text-2xl font-bold text-gray-900">Mis Favoritos</h1>
      </div>

      {error && (
        <div className="mb-6 bg-red-50 border border-red-100 text-red-600 rounded-xl px-4 py-3 text-sm">
          {error}
        </div>
      )}

      {properties.length === 0 ? (
        <div className="text-center py-20">
          <Heart size={48} className="mx-auto text-gray-200 mb-4" />
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Tu lista de favoritos está vacía</h3>
          <p className="text-gray-500 mb-6">Guarda propiedades que te gusten para verlas aquí</p>
          <Link
            to="/"
            className="inline-block px-6 py-2.5 rounded-xl text-white text-sm font-medium transition-colors"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
          >
            Explorar propiedades
          </Link>
        </div>
      ) : (
        <>
          <p className="text-gray-500 text-sm mb-6">
            {properties.length} {properties.length === 1 ? 'propiedad guardada' : 'propiedades guardadas'}
          </p>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {properties.map(p => (
              <PropertyCard
                key={p.id}
                property={p}
                isInWishlist={true}
                onWishlistToggle={(id) => handleRemove(id)}
              />
            ))}
          </div>
        </>
      )}
    </div>
  )
}
