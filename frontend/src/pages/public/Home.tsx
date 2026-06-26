import { useState, useEffect, useCallback } from 'react'
import { Search, ChevronLeft, ChevronRight } from 'lucide-react'
import api from '@/api/axios'
import type { PagedResult, PropertyDto } from '@/types'
import PropertyCard from '@/components/shared/PropertyCard'
import LoadingSpinner from '@/components/shared/LoadingSpinner'
import { useAuth } from '@/hooks/useAuth'

const PAGE_SIZE = 12

interface Filters {
  city: string
  checkIn: string
  checkOut: string
  maxGuests: string
}

export default function Home() {
  const { isGuest, token } = useAuth()
  const [filters, setFilters] = useState<Filters>({ city: '', checkIn: '', checkOut: '', maxGuests: '' })
  const [applied, setApplied] = useState<Filters>({ city: '', checkIn: '', checkOut: '', maxGuests: '' })
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<PropertyDto> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [wishlistIds, setWishlistIds] = useState<Set<string>>(new Set())

  useEffect(() => {
    if (!isGuest || !token) return
    api.get<PropertyDto[]>('/wishlist')
      .then(r => setWishlistIds(new Set(r.data.map(p => p.id))))
      .catch(() => { /* silent */ })
  }, [isGuest, token])

  const loadProperties = useCallback(async (f: Filters, p: number) => {
    setLoading(true)
    setError('')
    try {
      const params = new URLSearchParams()
      if (f.city) params.set('city', f.city)
      if (f.checkIn) params.set('checkIn', f.checkIn)
      if (f.checkOut) params.set('checkOut', f.checkOut)
      if (f.maxGuests) params.set('maxGuests', f.maxGuests)
      params.set('page', String(p))
      params.set('pageSize', String(PAGE_SIZE))
      const { data } = await api.get<PagedResult<PropertyDto>>(`/properties?${params}`)
      setResult(data)
    } catch {
      setError('No se pudieron cargar las propiedades. Intenta de nuevo.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadProperties(applied, page)
  }, [applied, page, loadProperties])

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    setPage(1)
    setApplied({ ...filters })
  }

  const handleWishlistToggle = async (propertyId: string, inWishlist: boolean) => {
    try {
      if (inWishlist) {
        await api.delete(`/wishlist/${propertyId}`)
        setWishlistIds(prev => { const s = new Set(prev); s.delete(propertyId); return s })
      } else {
        await api.post(`/wishlist/${propertyId}`)
        setWishlistIds(prev => new Set([...prev, propertyId]))
      }
    } catch {
      // silent
    }
  }

  return (
    <div>
      {/* Hero */}
      <section className="bg-[#2D6A4F] text-white py-16 px-4">
        <div className="max-w-3xl mx-auto text-center">
          <h1 className="text-4xl sm:text-5xl font-bold mb-3">Encuentra tu lugar perfecto</h1>
          <p className="text-[#b7dac9] text-lg">Descubre alojamientos únicos para cada tipo de viaje</p>
        </div>

        {/* Search form */}
        <form
          onSubmit={handleSearch}
          className="mt-8 max-w-4xl mx-auto bg-white rounded-2xl shadow-xl p-4 flex flex-col sm:flex-row gap-3"
        >
          <div className="flex-1">
            <label className="block text-xs font-medium text-gray-500 mb-1">Ciudad</label>
            <input
              type="text"
              value={filters.city}
              onChange={e => setFilters(f => ({ ...f, city: e.target.value }))}
              placeholder="¿A dónde vas?"
              className="w-full text-sm text-gray-900 border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
            />
          </div>
          <div className="sm:w-36">
            <label className="block text-xs font-medium text-gray-500 mb-1">Check-in</label>
            <input
              type="date"
              value={filters.checkIn}
              onChange={e => setFilters(f => ({ ...f, checkIn: e.target.value }))}
              className="w-full text-sm text-gray-900 border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
            />
          </div>
          <div className="sm:w-36">
            <label className="block text-xs font-medium text-gray-500 mb-1">Check-out</label>
            <input
              type="date"
              value={filters.checkOut}
              onChange={e => setFilters(f => ({ ...f, checkOut: e.target.value }))}
              className="w-full text-sm text-gray-900 border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
            />
          </div>
          <div className="sm:w-28">
            <label className="block text-xs font-medium text-gray-500 mb-1">Huéspedes</label>
            <input
              type="number"
              min="1"
              value={filters.maxGuests}
              onChange={e => setFilters(f => ({ ...f, maxGuests: e.target.value }))}
              placeholder="1"
              className="w-full text-sm text-gray-900 border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
            />
          </div>
          <div className="flex items-end">
            <button
              type="submit"
              className="w-full sm:w-auto px-6 py-2 rounded-lg text-white font-medium text-sm flex items-center gap-2 transition-colors"
              style={{ backgroundColor: '#2D6A4F' }}
              onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
              onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
            >
              <Search size={16} />
              Buscar
            </button>
          </div>
        </form>
      </section>

      {/* Catalog */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        {loading ? (
          <LoadingSpinner />
        ) : error ? (
          <div className="text-center py-16 text-red-500">{error}</div>
        ) : !result || result.items.length === 0 ? (
          <div className="text-center py-16">
            <div className="text-5xl mb-4">🔍</div>
            <h3 className="text-lg font-semibold text-gray-800">No se encontraron propiedades</h3>
            <p className="text-gray-500 mt-1">Intenta con otros filtros</p>
          </div>
        ) : (
          <>
            <div className="flex items-center justify-between mb-6">
              <p className="text-gray-600 text-sm">
                <span className="font-semibold text-gray-900">{result.totalCount}</span> propiedades encontradas
              </p>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {result.items.map(p => (
                <PropertyCard
                  key={p.id}
                  property={p}
                  isInWishlist={wishlistIds.has(p.id)}
                  onWishlistToggle={handleWishlistToggle}
                />
              ))}
            </div>

            {result.totalPages > 1 && (
              <div className="flex items-center justify-center gap-4 mt-10">
                <button
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="flex items-center gap-1 px-4 py-2 rounded-lg border border-gray-200 text-sm font-medium text-gray-700 disabled:opacity-40 hover:bg-gray-50 transition-colors"
                >
                  <ChevronLeft size={16} />
                  Anterior
                </button>
                <span className="text-sm text-gray-500">
                  Página <span className="font-semibold text-gray-900">{page}</span> de {result.totalPages}
                </span>
                <button
                  onClick={() => setPage(p => Math.min(result.totalPages, p + 1))}
                  disabled={page === result.totalPages}
                  className="flex items-center gap-1 px-4 py-2 rounded-lg border border-gray-200 text-sm font-medium text-gray-700 disabled:opacity-40 hover:bg-gray-50 transition-colors"
                >
                  Siguiente
                  <ChevronRight size={16} />
                </button>
              </div>
            )}
          </>
        )}
      </section>
    </div>
  )
}
