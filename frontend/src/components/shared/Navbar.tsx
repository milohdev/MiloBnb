import { Link, useNavigate, useLocation } from 'react-router-dom'
import { LogOut, Menu, X, Bell } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '@/hooks/useAuth'
import api from '@/api/axios'
import type { NotificationDto } from '@/types'

export default function Navbar() {
  const { user, isAuthenticated, isOwner, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const [unreadCount, setUnreadCount] = useState(0)

  useEffect(() => {
    if (!isAuthenticated || isOwner) return
    api.get<NotificationDto[]>('/notifications/unread')
      .then(r => setUnreadCount(r.data.length))
      .catch(() => { /* silent */ })
  }, [isAuthenticated, isOwner, location.pathname])

  const handleLogout = () => {
    logout()
    navigate('/')
    setMenuOpen(false)
  }

  const initials = user
    ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
    : ''

  return (
    <header className="sticky top-0 z-50 bg-white border-b border-gray-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          <Link to="/" className="text-2xl font-bold" style={{ color: '#2D6A4F' }}>
            MiloBnb
          </Link>

          <nav className="hidden md:flex items-center gap-6">
            {!isAuthenticated && (
              <>
                <Link to="/login" className="text-sm font-medium text-gray-700 hover:text-[#2D6A4F] transition-colors">
                  Iniciar sesión
                </Link>
                <Link
                  to="/register"
                  className="text-sm font-medium px-4 py-2 rounded-lg text-white transition-colors"
                  style={{ backgroundColor: '#2D6A4F' }}
                  onMouseEnter={e => (e.currentTarget.style.backgroundColor = '#265c44')}
                  onMouseLeave={e => (e.currentTarget.style.backgroundColor = '#2D6A4F')}
                >
                  Registrarse
                </Link>
              </>
            )}

            {isAuthenticated && !isOwner && (
              <>
                <Link to="/wishlist" className="text-sm font-medium text-gray-700 hover:text-[#2D6A4F] transition-colors">Favoritos</Link>
                <Link to="/reservations" className="text-sm font-medium text-gray-700 hover:text-[#2D6A4F] transition-colors">Mis Reservas</Link>
                <Link to="/notifications" className="relative text-sm font-medium text-gray-700 hover:text-[#2D6A4F] transition-colors flex items-center gap-1">
                  <Bell size={15} />
                  Notificaciones
                  {unreadCount > 0 && (
                    <span className="absolute -top-2 -right-3 bg-red-500 text-white text-[10px] font-bold w-4 h-4 rounded-full flex items-center justify-center">
                      {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                  )}
                </Link>
              </>
            )}

            {isAuthenticated && isOwner && (
              <>
                <Link to="/my-properties" className="text-sm font-medium text-gray-700 hover:text-[#2D6A4F] transition-colors">Mis Inmuebles</Link>
                <Link to="/dashboard" className="text-sm font-medium text-gray-700 hover:text-[#2D6A4F] transition-colors">Dashboard</Link>
                <Link to="/reports" className="text-sm font-medium text-gray-700 hover:text-[#2D6A4F] transition-colors">Reportes</Link>
              </>
            )}

            {isAuthenticated && (
              <div className="flex items-center gap-3">
                <div
                  className="w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-bold"
                  style={{ backgroundColor: '#2D6A4F' }}
                >
                  {initials}
                </div>
                <button
                  onClick={handleLogout}
                  className="flex items-center gap-1 text-sm text-gray-500 hover:text-red-500 transition-colors"
                >
                  <LogOut size={16} />
                  Salir
                </button>
              </div>
            )}
          </nav>

          <button
            className="md:hidden p-2 rounded-md text-gray-500"
            onClick={() => setMenuOpen(!menuOpen)}
          >
            {menuOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>
      </div>

      {menuOpen && (
        <div className="md:hidden border-t border-gray-100 bg-white px-4 py-4 flex flex-col gap-4">
          {!isAuthenticated && (
            <>
              <Link to="/login" className="text-sm font-medium text-gray-700" onClick={() => setMenuOpen(false)}>Iniciar sesión</Link>
              <Link to="/register" className="text-sm font-medium text-[#2D6A4F]" onClick={() => setMenuOpen(false)}>Registrarse</Link>
            </>
          )}
          {isAuthenticated && !isOwner && (
            <>
              <Link to="/wishlist" className="text-sm font-medium text-gray-700" onClick={() => setMenuOpen(false)}>Favoritos</Link>
              <Link to="/reservations" className="text-sm font-medium text-gray-700" onClick={() => setMenuOpen(false)}>Mis Reservas</Link>
              <Link to="/notifications" className="flex items-center gap-2 text-sm font-medium text-gray-700" onClick={() => setMenuOpen(false)}>
                <span>Notificaciones</span>
                {unreadCount > 0 && (
                  <span className="bg-red-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full">
                    {unreadCount > 9 ? '9+' : unreadCount}
                  </span>
                )}
              </Link>
            </>
          )}
          {isAuthenticated && isOwner && (
            <>
              <Link to="/my-properties" className="text-sm font-medium text-gray-700" onClick={() => setMenuOpen(false)}>Mis Inmuebles</Link>
              <Link to="/dashboard" className="text-sm font-medium text-gray-700" onClick={() => setMenuOpen(false)}>Dashboard</Link>
              <Link to="/reports" className="text-sm font-medium text-gray-700" onClick={() => setMenuOpen(false)}>Reportes</Link>
            </>
          )}
          {isAuthenticated && (
            <button onClick={handleLogout} className="text-sm text-red-500 text-left flex items-center gap-2">
              <LogOut size={16} /> Cerrar sesión
            </button>
          )}
        </div>
      )}
    </header>
  )
}
