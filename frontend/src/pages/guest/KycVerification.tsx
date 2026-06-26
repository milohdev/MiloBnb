import { useEffect, useState } from 'react'
import { CheckCircle, ShieldCheck, AlertCircle, Loader2 } from 'lucide-react'
import api from '@/api/axios'
import type { KycVerificationDto } from '@/types'
import LoadingSpinner from '@/components/shared/LoadingSpinner'
import { useAuth } from '@/hooks/useAuth'

export default function KycVerification() {
  const { refreshKycStatus } = useAuth()
  const [kycStatus, setKycStatus] = useState<KycVerificationDto | null>(null)
  const [statusLoaded, setStatusLoaded] = useState(false)
  const [imageUrl, setImageUrl] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

  useEffect(() => {
    api.get<KycVerificationDto>('/kyc/status')
      .then(r => setKycStatus(r.data))
      .catch(() => { /* 404 means no verification yet */ })
      .finally(() => setStatusLoaded(true))
  }, [])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitError('')
    setSubmitting(true)
    try {
      const { data } = await api.post<KycVerificationDto>('/kyc/verify', { imageUrl })
      setKycStatus(data)
      if (data.status === 'Approved') {
        await refreshKycStatus()
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { title?: string } } })?.response?.data?.title
      setSubmitError(msg ?? 'Ocurrió un error al procesar la verificación.')
    } finally {
      setSubmitting(false)
    }
  }

  if (!statusLoaded) return <LoadingSpinner />

  const isApproved = kycStatus?.status === 'Approved'
  const isRejected = kycStatus?.status === 'Rejected'
  const showForm = !isApproved

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <div className="flex items-center gap-3 mb-2">
        <ShieldCheck size={24} className="text-[#2D6A4F]" />
        <h1 className="text-2xl font-bold text-gray-900">Verificación de identidad</h1>
      </div>
      <p className="text-gray-500 text-sm mb-8">
        Para poder hacer reservas en MiloBnb necesitamos verificar tu identidad.
        Este proceso nos ayuda a garantizar la seguridad de todos los usuarios de la plataforma.
      </p>

      {isApproved && (
        <div className="bg-green-50 border border-green-200 rounded-2xl p-8 text-center">
          <CheckCircle size={48} className="mx-auto text-green-500 mb-4" />
          <h2 className="text-lg font-semibold text-green-800 mb-2">¡Identidad verificada!</h2>
          <p className="text-green-700 text-sm">
            Tu identidad fue verificada exitosamente. Ya puedes hacer reservas en MiloBnb.
          </p>
          {kycStatus.extractedFirstName && (
            <div className="mt-4 bg-white rounded-xl p-4 text-left text-sm text-gray-600 space-y-1">
              <p><span className="font-medium">Nombre:</span> {kycStatus.extractedFirstName} {kycStatus.extractedLastName}</p>
              {kycStatus.extractedDocumentNumber && (
                <p><span className="font-medium">Documento:</span> {kycStatus.extractedDocumentNumber}</p>
              )}
            </div>
          )}
        </div>
      )}

      {isRejected && (
        <div className="bg-red-50 border border-red-200 rounded-2xl p-5 mb-6">
          <div className="flex items-start gap-3">
            <AlertCircle size={20} className="text-red-500 shrink-0 mt-0.5" />
            <div>
              <p className="font-semibold text-red-800">Verificación rechazada</p>
              <p className="text-red-700 text-sm mt-1">
                {kycStatus.rejectionReason ?? 'No pudimos verificar tu identidad con el documento proporcionado.'}
              </p>
            </div>
          </div>
        </div>
      )}

      {showForm && (
        <div className="bg-white border border-gray-100 rounded-2xl p-6 shadow-sm">
          <h2 className="font-semibold text-gray-900 mb-1">
            {isRejected ? 'Intentar de nuevo' : 'Sube tu documento de identidad'}
          </h2>
          <p className="text-gray-500 text-sm mb-5">
            Proporciona una URL pública de una imagen de tu documento de identidad (cédula, pasaporte o licencia de conducir).
            La imagen debe mostrar claramente tu nombre, apellido y número de documento.
          </p>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="imageUrl" className="block text-sm font-medium text-gray-700 mb-1">
                URL de la imagen del documento
              </label>
              <input
                id="imageUrl"
                type="url"
                required
                value={imageUrl}
                onChange={e => { setImageUrl(e.target.value); setSubmitError('') }}
                placeholder="https://ejemplo.com/mi-documento.jpg"
                className="w-full border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-[#2D6A4F] transition"
              />
              <p className="text-gray-400 text-xs mt-1">
                Puedes subir la imagen a cualquier servicio de almacenamiento y copiar la URL pública aquí.
              </p>
            </div>

            {submitError && (
              <div className="bg-red-50 border border-red-100 rounded-xl p-3 text-red-600 text-sm">
                {submitError}
              </div>
            )}

            <button
              type="submit"
              disabled={submitting || !imageUrl}
              className="w-full py-3 rounded-xl text-white text-sm font-medium transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
              style={{ backgroundColor: '#2D6A4F' }}
              onMouseEnter={e => !submitting && (e.currentTarget.style.backgroundColor = '#265c44')}
              onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
            >
              {submitting ? (
                <>
                  <Loader2 size={16} className="animate-spin" />
                  Verificando tu identidad...
                </>
              ) : (
                <>
                  <ShieldCheck size={16} />
                  {isRejected ? 'Reintentar verificación' : 'Verificar identidad'}
                </>
              )}
            </button>
          </form>
        </div>
      )}
    </div>
  )
}
