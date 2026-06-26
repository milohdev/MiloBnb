import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import type { ReactNode } from 'react'
import Navbar from '@/components/shared/Navbar'
import Footer from '@/components/shared/Footer'

import Home from '@/pages/public/Home'
import PropertyDetail from '@/pages/public/PropertyDetail'
import Login from '@/pages/auth/Login'
import Register from '@/pages/auth/Register'

import MyReservations from '@/pages/guest/MyReservations'
import Wishlist from '@/pages/guest/Wishlist'
import KycVerification from '@/pages/guest/KycVerification'
import Notifications from '@/pages/guest/Notifications'

import MyProperties from '@/pages/owner/MyProperties'
import CreateProperty from '@/pages/owner/CreateProperty'
import EditProperty from '@/pages/owner/EditProperty'
import Dashboard from '@/pages/owner/Dashboard'
import Reports from '@/pages/owner/Reports'

function GuestRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isOwner } = useAuth()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (isOwner) return <Navigate to="/" replace />
  return <>{children}</>
}

function OwnerRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isOwner } = useAuth()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!isOwner) return <Navigate to="/" replace />
  return <>{children}</>
}

function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1">{children}</main>
      <Footer />
    </div>
  )
}

export default function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout><Home /></Layout>} />
        <Route path="/properties/:id" element={<Layout><PropertyDetail /></Layout>} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />

        <Route path="/reservations" element={<GuestRoute><Layout><MyReservations /></Layout></GuestRoute>} />
        <Route path="/wishlist" element={<GuestRoute><Layout><Wishlist /></Layout></GuestRoute>} />
        <Route path="/kyc" element={<GuestRoute><Layout><KycVerification /></Layout></GuestRoute>} />
        <Route path="/notifications" element={<GuestRoute><Layout><Notifications /></Layout></GuestRoute>} />

        <Route path="/my-properties" element={<OwnerRoute><Layout><MyProperties /></Layout></OwnerRoute>} />
        <Route path="/my-properties/create" element={<OwnerRoute><Layout><CreateProperty /></Layout></OwnerRoute>} />
        <Route path="/my-properties/:id/edit" element={<OwnerRoute><Layout><EditProperty /></Layout></OwnerRoute>} />
        <Route path="/dashboard" element={<OwnerRoute><Layout><Dashboard /></Layout></OwnerRoute>} />
        <Route path="/reports" element={<OwnerRoute><Layout><Reports /></Layout></OwnerRoute>} />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
