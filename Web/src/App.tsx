import { useAuth } from './auth/AuthContext'
import { AuthPage } from './components/AuthPage'
import { Messenger } from './components/Messenger'

export default function App() {
  const { me, loading } = useAuth()

  if (loading) return <div className="flex h-full items-center justify-center text-porcelain/60">Загрузка…</div>
  return me ? <Messenger /> : <AuthPage />
}
