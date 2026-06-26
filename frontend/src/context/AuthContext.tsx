import { createContext, useCallback, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import api from '@/api/axios'
import type { AuthResponse, CurrentUserDto, LoginRequest, RegisterRequest, UserDto } from '@/types'

interface AuthContextValue {
  user: UserDto | null
  token: string | null
  login: (email: string, password: string) => Promise<void>
  register: (data: RegisterRequest) => Promise<void>
  logout: () => void
  refreshKycStatus: () => Promise<void>
  isAuthenticated: boolean
  isOwner: boolean
  isGuest: boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)

function mapAuthResponse(data: AuthResponse): UserDto {
  return {
    id: data.userId,
    email: data.email,
    firstName: data.firstName,
    lastName: data.lastName,
    role: data.role,
    isKycVerified: false,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(() => {
    const stored = localStorage.getItem('user')
    return stored ? (JSON.parse(stored) as UserDto) : null
  })
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'))

  useEffect(() => {
    if (token) localStorage.setItem('token', token)
    else localStorage.removeItem('token')
  }, [token])

  useEffect(() => {
    if (user) localStorage.setItem('user', JSON.stringify(user))
    else localStorage.removeItem('user')
  }, [user])

  const login = useCallback(async (email: string, password: string) => {
    const payload: LoginRequest = { email, password }
    const { data } = await api.post<AuthResponse>('/auth/login', payload)
    setToken(data.token)
    setUser(mapAuthResponse(data))
  }, [])

  const register = useCallback(async (data: RegisterRequest) => {
    const { data: response } = await api.post<AuthResponse>('/auth/register', data)
    setToken(response.token)
    setUser(mapAuthResponse(response))
  }, [])

  const logout = useCallback(() => {
    setToken(null)
    setUser(null)
  }, [])

  const refreshKycStatus = useCallback(async () => {
    try {
      const { data } = await api.get<CurrentUserDto>('/auth/me')
      setUser(prev => prev ? { ...prev, isKycVerified: data.isKycVerified } : prev)
    } catch {
      // silently fail
    }
  }, [])

  const value: AuthContextValue = {
    user,
    token,
    login,
    register,
    logout,
    refreshKycStatus,
    isAuthenticated: !!token,
    isOwner: user?.role === 'Owner',
    isGuest: user?.role === 'Guest',
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
