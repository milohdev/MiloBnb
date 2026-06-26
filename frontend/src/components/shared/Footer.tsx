export default function Footer() {
  return (
    <footer className="bg-gray-50 border-t border-gray-200 mt-auto">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex flex-col sm:flex-row justify-between items-center gap-4">
          <span className="text-xl font-bold" style={{ color: '#2D6A4F' }}>MiloBnb</span>
          <p className="text-sm text-gray-500">© {new Date().getFullYear()} MiloBnb. Todos los derechos reservados.</p>
        </div>
      </div>
    </footer>
  )
}
