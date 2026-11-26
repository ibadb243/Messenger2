import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, setUnauthorizedHandler, tokenStore } from '../api/client'
import type { AuthResponse, Me } from '../api/types'

interface AuthState {
  me: Me | null
  loading: boolean
  signIn: (response: AuthResponse) => void
  signOut: () => void
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [me, setMe] = useState<Me | null>(null)
  const [loading, setLoading] = useState(() => tokenStore.get() !== null)

  const signOut = useCallback(() => {
    tokenStore.clear()
    setMe(null)
  }, [])

  const signIn = useCallback((response: AuthResponse) => {
    tokenStore.set(response.token)
    setMe(response.user)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(signOut)
    if (!tokenStore.get()) return
    api.me().then(setMe).catch(signOut).finally(() => setLoading(false))
  }, [signOut])

  return <AuthContext value={{ me, loading, signIn, signOut }}>{children}</AuthContext>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth вне AuthProvider')
  return ctx
}
