import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { User } from '../api/types'
import { fullName } from '../lib/format'
import { Avatar, ErrorText, inputClass } from './ui'

const MIN_QUERY = 2
const DEBOUNCE_MS = 300

/** Поиск пользователей по имени или email. Уже выбранных (excludeIds) в списке не показываем. */
export function UserSearch({ onPick, excludeIds = [] }: { onPick: (user: User) => void; excludeIds?: number[] }) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<User[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    if (query.trim().length < MIN_QUERY) {
      setResults([])
      return
    }
    let cancelled = false
    const timer = setTimeout(() => {
      api.searchUsers(query.trim())
        .then((users) => { if (!cancelled) { setResults(users); setError('') } })
        .catch((e: Error) => { if (!cancelled) setError(e.message) })
    }, DEBOUNCE_MS)
    return () => { cancelled = true; clearTimeout(timer) }
  }, [query])

  const visible = results.filter((u) => !excludeIds.includes(u.id))

  return (
    <div className="flex flex-col gap-2">
      <input className={inputClass} placeholder="Найти по имени или email…" value={query}
        onChange={(e) => setQuery(e.target.value)} autoFocus />
      <ErrorText>{error}</ErrorText>
      <ul className="flex max-h-56 flex-col overflow-y-auto">
        {visible.map((u) => (
          <li key={u.id}>
            <button className="flex w-full items-center gap-3 rounded-lg px-2 py-2 text-left hover:bg-ink/40"
              onClick={() => onPick(u)}>
              <Avatar name={fullName(u)} size={32} />
              <span>{fullName(u)}</span>
            </button>
          </li>
        ))}
        {query.trim().length >= MIN_QUERY && visible.length === 0 && !error && (
          <li className="px-2 py-2 text-sm text-porcelain/60">Никого не найдено</li>
        )}
      </ul>
    </div>
  )
}
