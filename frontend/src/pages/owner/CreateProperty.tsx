import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Trash2, ArrowRight, ImageIcon } from 'lucide-react'
import api from '@/api/axios'
import type { CreatePropertyRequest, PropertyDto } from '@/types'

function formatCOP(amount: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency', currency: 'COP',
    minimumFractionDigits: 0, maximumFractionDigits: 0,
  }).format(amount)
}

type Step = 'form' | 'images'

interface ImageEntry {
  id: string
  url: string
}

export default function CreateProperty() {
  const navigate = useNavigate()
  const [step, setStep] = useState<Step>('form')
  const [createdProperty, setCreatedProperty] = useState<PropertyDto | null>(null)
  const [images, setImages] = useState<ImageEntry[]>([])
  const [imageUrl, setImageUrl] = useState('')
  const [addingImage, setAddingImage] = useState(false)

  const [form, setForm] = useState<CreatePropertyRequest>({
    name: '', description: '', address: '', city: '', country: '',
    pricePerNight: 0, maxGuests: 1, bedrooms: 1, bathrooms: 1,
    allowSameDayBooking: false,
  })
  const [errors, setErrors] = useState<Partial<Record<keyof CreatePropertyRequest, string>>>({})
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

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
      const { data } = await api.post<PropertyDto>('/properties', form)
      setCreatedProperty(data)
      setStep('images')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { title?: string } } })?.response?.data?.title
      setSubmitError(msg ?? 'No se pudo crear la propiedad.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleAddImage = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!createdProperty || !imageUrl.trim()) return
    setAddingImage(true)
    try {
      const { data } = await api.post<{ imageId: string }>(`/properties/${createdProperty.id}/images`, { url: imageUrl })
      setImages(prev => [...prev, { id: data.imageId, url: imageUrl }])
      setImageUrl('')
    } catch {
      // silent
    } finally {
      setAddingImage(false)
    }
  }

  const handleRemoveImage = async (imageId: string) => {
    if (!createdProperty) return
    try {
      await api.delete(`/properties/${createdProperty.id}/images/${imageId}`)
      setImages(prev => prev.filter(i => i.id !== imageId))
    } catch {
      // silent
    }
  }

  const inputClass = (field: keyof CreatePropertyRequest) =>
    `w-full border rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F] transition ${
      errors[field] ? 'border-red-300' : 'border-gray-200'
    }`

  if (step === 'images' && createdProperty) {
    return (
      <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        <div className="flex items-center gap-3 mb-2">
          <ImageIcon size={22} className="text-[#2D6A4F]" />
          <h1 className="text-2xl font-bold text-gray-900">Imágenes de {createdProperty.name}</h1>
        </div>
        <p className="text-gray-500 text-sm mb-8">
          Agrega imágenes para que los huéspedes conozcan tu propiedad. Puedes hacerlo ahora o más tarde desde Editar.
        </p>

        <form onSubmit={handleAddImage} className="flex gap-3 mb-6">
          <input
            type="url"
            value={imageUrl}
            onChange={e => setImageUrl(e.target.value)}
            placeholder="https://ejemplo.com/imagen.jpg"
            className="flex-1 border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F]"
          />
          <button
            type="submit"
            disabled={addingImage || !imageUrl.trim()}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-white text-sm font-medium disabled:opacity-50 transition-colors"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => !addingImage && (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
          >
            <Plus size={16} />
            Agregar
          </button>
        </form>

        {images.length > 0 && (
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 mb-8">
            {images.map(img => (
              <div key={img.id} className="relative group aspect-[4/3] rounded-xl overflow-hidden bg-gray-100">
                <img src={img.url} alt="" className="w-full h-full object-cover" />
                <button
                  onClick={() => handleRemoveImage(img.id)}
                  className="absolute top-2 right-2 p-1.5 bg-white/90 rounded-full text-red-500 hover:bg-red-50 opacity-0 group-hover:opacity-100 transition-opacity"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
        )}

        <button
          onClick={() => navigate('/my-properties')}
          className="flex items-center gap-2 px-6 py-3 rounded-xl text-white font-medium transition-colors"
          style={{ backgroundColor: '#2D6A4F' }}
          onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
          onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
        >
          Finalizar
          <ArrowRight size={16} />
        </button>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <h1 className="text-2xl font-bold text-gray-900 mb-8">Nueva propiedad</h1>

      <form onSubmit={handleSubmit} className="space-y-5 bg-white border border-gray-100 rounded-2xl p-6 shadow-sm">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Nombre *</label>
          <input name="name" value={form.name} onChange={handleChange} className={inputClass('name')} placeholder="Apartamento moderno en El Poblado" />
          {errors.name && <p className="text-red-500 text-xs mt-1">{errors.name}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Descripción *</label>
          <textarea
            name="description"
            value={form.description}
            onChange={handleChange}
            rows={4}
            className={`${inputClass('description')} resize-none`}
            placeholder="Describe tu propiedad detalladamente..."
          />
          <div className="flex justify-between mt-1">
            {errors.description ? <p className="text-red-500 text-xs">{errors.description}</p> : <span />}
            <span className="text-gray-400 text-xs">{form.description.length}/2000</span>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Dirección *</label>
            <input name="address" value={form.address} onChange={handleChange} className={inputClass('address')} placeholder="Calle 10 # 38-15" />
            {errors.address && <p className="text-red-500 text-xs mt-1">{errors.address}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Ciudad *</label>
            <input name="city" value={form.city} onChange={handleChange} className={inputClass('city')} placeholder="Medellín" />
            {errors.city && <p className="text-red-500 text-xs mt-1">{errors.city}</p>}
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">País *</label>
          <input name="country" value={form.country} onChange={handleChange} className={inputClass('country')} placeholder="Colombia" />
          {errors.country && <p className="text-red-500 text-xs mt-1">{errors.country}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Precio por noche (COP) *
            {form.pricePerNight > 0 && <span className="text-gray-400 font-normal ml-2">{formatCOP(form.pricePerNight)}</span>}
          </label>
          <input
            type="number"
            name="pricePerNight"
            value={form.pricePerNight || ''}
            onChange={handleChange}
            min="1"
            className={inputClass('pricePerNight')}
            placeholder="150000"
          />
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

        <div className="flex gap-3 pt-2">
          <button
            type="button"
            onClick={() => navigate('/my-properties')}
            className="px-6 py-2.5 rounded-xl border border-gray-200 text-gray-700 text-sm font-medium hover:bg-gray-50 transition-colors"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 py-2.5 rounded-xl text-white text-sm font-medium transition-colors disabled:opacity-50"
            style={{ backgroundColor: '#2D6A4F' }}
            onMouseEnter={e => !submitting && (e.currentTarget.style.backgroundColor = '#265c44')}
            onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
          >
            {submitting ? 'Creando...' : 'Crear propiedad'}
          </button>
        </div>
      </form>
    </div>
  )
}
