import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { ErrorText, inputClass, primaryButton } from './ui'

export function AuthPage() {
  const { signIn } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [form, setForm] = useState({ email: '', password: '', firstName: '', lastName: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }))

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      const response = mode === 'login'
        ? await api.login(form.email, form.password)
        : await api.register(form.email, form.password, form.firstName, form.lastName)
      signIn(response)
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex h-full items-center justify-center p-4">
      <form onSubmit={submit} className="flex w-full max-w-sm flex-col gap-3 rounded-2xl bg-yale p-6 shadow-xl">
        <h1 className="text-2xl font-semibold">Messenger<span className="text-blush">2</span></h1>
        <p className="text-sm text-porcelain/70">
          {mode === 'login' ? 'Войдите, чтобы продолжить' : 'Создайте аккаунт'}
        </p>

        {mode === 'register' && (
          <div className="grid grid-cols-2 gap-3">
            <input className={inputClass} placeholder="Имя" value={form.firstName} onChange={set('firstName')} required />
            <input className={inputClass} placeholder="Фамилия" value={form.lastName} onChange={set('lastName')} required />
          </div>
        )}
        <input className={inputClass} type="email" placeholder="Email" autoComplete="email"
          value={form.email} onChange={set('email')} required />
        <input className={inputClass} type="password" placeholder="Пароль (от 8 символов)"
          autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
          value={form.password} onChange={set('password')} required minLength={8} />

        <ErrorText>{error}</ErrorText>

        <button className={primaryButton} disabled={busy}>
          {busy ? 'Подождите…' : mode === 'login' ? 'Войти' : 'Зарегистрироваться'}
        </button>

        <button type="button" className="text-sm text-sage hover:underline"
          onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setError('') }}>
          {mode === 'login' ? 'Нет аккаунта? Зарегистрируйтесь' : 'Уже есть аккаунт? Войдите'}
        </button>
      </form>
    </div>
  )
}
