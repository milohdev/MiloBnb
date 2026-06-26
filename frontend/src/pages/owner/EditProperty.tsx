import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Plus, Trash2 } from 'lucide-react'
import api from '@/api/axios'
import type { CreatePropertyRequest, PropertyDto } from '@/types'
import LoadingSpinner from '@/components/shared/LoadingSpinner'

function formatCOP(amount: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency', currency: 'COP',
    minimumFractionDigits: 0, maximumFractionDigits: 0,
  }).format(amount)
}

interface ImageEntry {
  id: string
  url: string
}

export default function EditProperty() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState<CreatePropertyRequest>({
    name: '', description: '', address: '', city: '', country: '',
    pricePerNight: 0, maxGuests: 1, bedrooms: 1, bathrooms: 1, allowSameDayBooking: false,
  })
  const [images, setImages] = useState<ImageEntry[]>([])
  const [imageUrl, setImageUrl] = useState('')
  const [addingImage, setAddingImage] = useState(false)
  const [errors, setErrors] = useState<Partial<Record<keyof CreatePropertyRequest, string>>>({})
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [saveSuccess, setSaveSuccess] = useState(false)

  useEffect(() => {
    if (!id) return
    api.get<PropertyDto>(`/properties/${id}`)
      .then(r => {
        const p = r.data
        setForm({
          name: p.name, description: p.description, address: p.address,
          city: p.city, country: p.country, pricePerNight: p.pricePerNight,
          maxGuests: p.maxGuests, bedrooms: p.bedrooms, bathrooms: p.bathrooms,
          allowSameDayBooking: p.allowSameDayBooking,
        })
        setImages(p.images.map((url, i) => ({ id: `img-${i}`, url })))
      })
      .catch(() => navigate('/my-properties'))
      .finally(() => setLoading(false))
  }, [id, navigate])

  const validate = (): boolean => {
    const e: Partial<Record<keyof CreatePropertyRequest, string>> = {}
    if (!form.name.trim()) e.name = 'El nombre es requerido'
    else if (form.name.length > 200) e.name = 'Máximo 200 caracteres'
    if (!form.description.trim()) e.description = 'La descripción es requerida'
    else if (form.description.length > 2000) e.description = 'Máximo 2000 caracteres'
    if (!form.address.trim()) e.address = 'La dirección es requerida'
    if (!form.city.trim()) e.city = 'La ciudad es requerida'
    if (!form.country.trim()) e.country = 'El país es requerido'
    if (form.pricePerNight <= 0) e.pricePerNight = 'El precio debe ser mayor a 0'
    if (form.maxGuests < 1 || form.maxGuests > 20) e.maxGuests = 'Entre 1 y 20 huéspedes'
    if (form.bedrooms < 0 || form.bedrooms > 20) e.bedrooms = 'Entre 0 y 20 habitaciones'
    if (form.bathrooms < 0 || form.bathrooms > 20) e.bathrooms = 'Entre 0 y 20 baños'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value, type } = e.target
    const checked = (e.target as HTMLInputElement).checked
    setForm(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : type === 'number' ? Number(value) : value,
    }))
    setErrors(prev => ({ ...prev, [name]: undefined }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validate()) return
    setSubmitError('')
    setSubmitting(true)
    try {
      await api.put(`/properties/${id}`, form)
      setSaveSuccess(true)
      setTimeout(() => navigate('/my-properties'), 1200)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { title?: string } } })?.response?.data?.title
      setSubmitError(msg ?? 'No se pudo guardar los cambios.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleAddImage = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!id || !imageUrl.trim()) return
    setAddingImage(true)
    try {
      const { data } = await api.post<{ imageId: string }>(`/properties/${id}/images`, { url: imageUrl })
      setImages(prev => [...prev, { id: data.imageId, url: imageUrl }])
      setImageUrl('')
    } catch {
      // silent
    } finally {
      setAddingImage(false)
    }
  }

  const handleRemoveImage = async (imageId: string) => {
    if (!id || imageId.startsWith('img-')) return
    try {
      await api.delete(`/properties/${id}/images/${imageId}`)
      setImages(prev => prev.filter(i => i.id !== imageId))
    } catch {
      // silent
    }
  }

  const inputClass = (field: keyof CreatePropertyRequest) =>
    `w-full border rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F] transition ${
      errors[field] ? 'border-red-300' : 'border-gray-200'
    }`

  if (loading) return <LoadingSpinner />

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <h1 className="text-2xl font-bold text-gray-900 mb-8">Editar propiedad</h1>

      <form onSubmit={handleSubmit} className="space-y-5 bg-white border border-gray-100 rounded-2xl p-6 shadow-sm mb-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Nombre *</label>
          <input name="name" value={form.name} onChange={handleChange} className={inputClass('name')} />
          {errors.name && <p className="text-red-500 text-xs mt-1">{errors.name}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Descripción *</label>
          <textarea name="description" value={form.description} onChange={handleChange} rows={4}
            className={`${inputClass('description')} resize-none`} />
          <div className="flex justify-between mt-1">
            {errors.description ? <p className="text-red-500 text-xs">{errors.description}</p> : <span />}
            <span className="text-gray-400 text-xs">{form.description.length}/2000</span>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Dirección *</label>
            <input name="address" value={form.address} onChange={handleChange} className={inputClass('address')} />
            {errors.address && <p className="text-red-500 text-xs mt-1">{errors.address}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Ciudad *</label>
            <input name="city" value={form.city} onChange={handleChange} className={inputClass('city')} />
            {errors.city && <p className="text-red-500 text-xs mt-1">{errors.city}</p>}
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">País *</label>
          <input name="country" value={form.country} onChange={handleChange} className={inputClass('country')} />
          {errors.country && <p className="text-red-500 text-xs mt-1">{errors.country}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Precio por noche (COP) *
            {form.pricePerNight > 0 && <span className="text-gray-400 font-normal ml-2">{formatCOP(form.pricePerNight)}</span>}
          </label>
          <input type="number" name="pricePerNight" value={form.pricePerNight || ''} onChange={handleChange} min="1" className={inputClass('pricePerNight')} />
          {errors.pricePerNight && <p className="text-red-500 text-xs mt-1">{errors.pricePerNight}</p>}
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Huéspedes *</label>
            <input type="number" name="maxGuests" value={form.maxGuests} onChange={handleChange} min="1" max="20" className={inputClass('maxGuests')} />
            {errors.maxGuests && <p className="text-red-500 text-xs mt-1">{errors.maxGuests}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Habitaciones *</label>
            <input type="number" name="bedrooms" value={form.bedrooms} onChange={handleChange} min="0" max="20" className={inputClass('bedrooms')} />
            {errors.bedrooms && <p className="text-red-500 text-xs mt-1">{errors.bedrooms}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Baños *</label>
            <input type="number" name="bathrooms" value={form.bathrooms} onChange={handleChange} min="0" max="20" className={inputClass('bathrooms')} />
            {errors.bathrooms && <p className="text-red-500 text-xs mt-1">{errors.bathrooms}</p>}
          </div>
        </div>

        <div className="flex items-center justify-between p-4 bg-gray-50 rounded-xl">
          <div>
            <p className="text-sm font-medium text-gray-900">Permitir reserva el mismo día</p>
            <p className="text-xs text-gray-500 mt-0.5">Los huéspedes podrán reservar con check-in hoy</p>
          </div>
          <button
            type="button"
            onClick={() => setForm(prev => ({ ...prev, allowSameDayBooking: !prev.allowSameDayBooking }))}
            className={`relative w-12 h-6 rounded-full transition-colors ${form.allowSameDayBooking ? 'bg-[#2D6A4F]' : 'bg-gray-300'}`}
          >
            <span className={`absolute top-1 w-4 h-4 rounded-full bg-white shadow transition-transform ${form.allowSameDayBooking ? 'translate-x-7' : 'translate-x-1'}`} />
          </button>
        </div>

        {submitError && (
          <div className="bg-red-50 border border-red-100 rounded-xl p-3 text-red-600 text-sm">{submitError}</div>
        )}
        {saveSuccess && (
          <div className="bg-green-50 border border-green-200 rounded-xl p-3 text-green-700 text-sm">¡Cambios guardados! Redirigiendo...</div>
        )}

        <div className="flex gap-3 pt-2">
          <button type="button" onClick={() => navigate('/my-properties')}
            className="px-6 py-2.5 rounded-xl border border-gray-200 text-gray-700 text-sm font-medium hover:bg-gray-50 transition-colors">
            Cancelar
          </button>
          <button type="submit" disabled={submitting}
            className="flex-1 py-2.5 rounded-xl text-white text-sm font-medium transition-colors disabled:opacity-50"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => !submitting && (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}>
            {submitting ? 'Guardando...' : 'Guardar cambios'}
          </button>
        </div>
      </form>

      {/* Images section */}
      <div className="bg-white border border-gray-100 rounded-2xl p-6 shadow-sm">
        <h2 className="font-semibold text-gray-900 mb-4">Imágenes</h2>
        <form onSubmit={handleAddImage} className="flex gap-3 mb-4">
          <input type="url" value={imageUrl} onChange={e => setImageUrl(e.target.value)}
            placeholder="https://ejemplo.com/imagen.jpg"
            className="flex-1 border border-gray-200 rounded-xl px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]" />
          <button type="submit" disabled={addingImage || !imageUrl.trim()}
            className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-white text-sm font-medium disabled:opacity-50 transition-colors"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => !addingImage && (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}>
            <Plus size={15} />
            Agregar
          </button>
        </form>
        {images.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {images.map(img => (
              <div key={img.id} className="relative group aspect-[4/3] rounded-xl overflow-hidden bg-gray-100">
                <img src={img.url} alt="" className="w-full h-full object-cover" />
                {!img.id.startsWith('img-') && (
                  <button onClick={() => handleRemoveImage(img.id)}
                    className="absolute top-2 right-2 p-1.5 bg-white/90 rounded-full text-red-500 hover:bg-red-50 opacity-0 group-hover:opacity-100 transition-opacity">
                    <Trash2 size={14} />
                  </button>
                )}
              </div>
            ))}
          </div>
        ) : (
          <p className="text-gray-400 text-sm">No hay imágenes. Agrega una URL arriba.</p>
        )}
      </div>
    </div>
  )
}
