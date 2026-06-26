// Auth
export interface AuthResponse {
  userId: string
  firstName: string
  lastName: string
  email: string
  role: 'Admin' | 'Guest' | 'Owner'
  token: string
  expiresAt: string
}

export interface CurrentUserDto {
  userId: string
  firstName: string
  lastName: string
  email: string
  role: 'Admin' | 'Guest' | 'Owner'
  isKycVerified: boolean
}

export interface UserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  role: 'Admin' | 'Guest' | 'Owner'
  isKycVerified: boolean
}

// Properties — backend uses "name" not "title", images are string[] of URLs
export interface PropertyDto {
  id: string
  name: string
  description: string
  address: string
  city: string
  country: string
  pricePerNight: number
  maxGuests: number
  bedrooms: number
  bathrooms: number
  allowSameDayBooking: boolean
  isActive: boolean
  ownerId: string
  images: string[]
  createdAt: string
}

// Reservations
export interface ReservationDto {
  id: string
  propertyId: string
  propertyName: string
  guestId: string
  guestFullName: string
  checkInDate: string
  checkOutDate: string
  checkInDateTime: string
  checkOutDateTime: string
  totalNights: number
  totalPrice: number
  status: string
}

// Notifications
export interface NotificationDto {
  id: string
  title: string
  body: string
  type: string
  isRead: boolean
  relatedEntityId?: string
  createdAt: string
}

// KYC
export interface KycVerificationDto {
  id: string
  userId: string
  status: string
  extractedFirstName?: string
  extractedLastName?: string
  extractedDocumentNumber?: string
  extractedBirthDate?: string
  rejectionReason?: string
  createdAt: string
}

// Owner Dashboard
export interface PropertySummaryDto {
  propertyId: string
  propertyName: string
  reservationCount: number
  revenue: number
}

export interface OwnerDashboardDto {
  totalProperties: number
  totalReservations: number
  totalRevenue: number
  occupancyRate: number
  averagePricePerNight: number
  dateFrom: string
  dateTo: string
  reservationsByProperty: PropertySummaryDto[]
}

export interface PropertyMetricsDto {
  propertyId: string
  propertyName: string
  pricePerNight: number
  allowSameDayBooking: boolean
  totalReservations: number
  totalRevenue: number
  occupancyRate: number
  averageLengthOfStay: number
  dateFrom: string
  dateTo: string
  recentReservations: ReservationDto[]
}

// Pagination
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// Requests
export interface RegisterRequest {
  firstName: string
  lastName: string
  email: string
  password: string
  role: 'Guest' | 'Owner'
}

export interface LoginRequest {
  email: string
  password: string
}

export interface CreateReservationRequest {
  propertyId: string
  checkInDate: string
  checkOutDate: string
}

export interface CreatePropertyRequest {
  name: string
  description: string
  address: string
  city: string
  country: string
  pricePerNight: number
  maxGuests: number
  bedrooms: number
  bathrooms: number
  allowSameDayBooking: boolean
}
