import { useCallback, useEffect, useRef, useState } from 'react'
import { api } from '../api/client'
import type { Message } from '../api/types'

const POLL_INTERVAL_MS = 3000
const PAGE_SIZE = 50
// Историю отдаёт Cassandra с задержкой синхронизации, поэтому локальные правки держим поверх неё.
const OVERRIDE_TTL_MS = 60_000

type Override = { kind: 'deleted' } | { kind: 'edited'; message: Message }
type Tracked = Override & { at: number }

/**
 * Сообщения открытого чата: первая страница, подгрузка старых, опрос новых и локальные
 * операции (отправка / правка / удаление). Сервер — источник истины, но пока синхронизация
 * не догнала запись, показываем локальное состояние (Tracked), чтобы интерфейс не "мигал".
 */
export function useMessages(chatId: number) {
  const [messages, setMessages] = useState<Message[]>([])
  const [loading, setLoading] = useState(true)
  const [hasMore, setHasMore] = useState(false)
  const [error, setError] = useState('')
  const overrides = useRef(new Map<number, Tracked>())

  /** Накладывает локальные правки на ответ сервера и убирает те, что сервер уже отразил. */
  const reconcile = useCallback((server: Message[], oldest: number) => {
    const now = Date.now()
    for (const [id, o] of overrides.current) {
      const fromServer = server.find((m) => m.id === id)
      const reflected = o.kind === 'deleted'
        ? !fromServer && id >= oldest
        : fromServer?.editedAt === o.message.editedAt
      if (reflected || now - o.at > OVERRIDE_TTL_MS) overrides.current.delete(id)
    }
    return (list: Message[]) => list.flatMap((m) => {
      const o = overrides.current.get(m.id)
      if (!o) return [m]
      return o.kind === 'deleted' ? [] : [o.message]
    })
  }, [])

  const fetchLatest = useCallback(async () => {
    const latestDesc = await api.messages(chatId, undefined, PAGE_SIZE)
    const latest = [...latestDesc].reverse()
    const apply = reconcile(latest, latest[0]?.id ?? Number.MAX_SAFE_INTEGER)

    setMessages((prev) => {
      const min = latest[0]?.id ?? Number.MAX_SAFE_INTEGER
      const max = latest.at(-1)?.id ?? -1
      const older = prev.filter((m) => m.id < min)
      const pending = prev.filter((m) => m.id > max) // отправлено нами, сервер ещё не видит
      return apply([...older, ...latest, ...pending])
    })
    return latestDesc.length
  }, [chatId, reconcile])

  // Смена чата: сбрасываем состояние и грузим первую страницу.
  useEffect(() => {
    let cancelled = false
    overrides.current.clear()
    setMessages([])
    setError('')
    setLoading(true)
    fetchLatest()
      .then((count) => { if (!cancelled) setHasMore(count === PAGE_SIZE) })
      .catch((e: Error) => { if (!cancelled) setError(e.message) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [fetchLatest])

  // Опрос новых сообщений.
  useEffect(() => {
    const timer = setInterval(() => { fetchLatest().catch(() => {}) }, POLL_INTERVAL_MS)
    return () => clearInterval(timer)
  }, [fetchLatest])

  const loadOlder = useCallback(async () => {
    const oldest = messages[0]?.id
    if (!oldest) return
    const olderDesc = await api.messages(chatId, oldest, PAGE_SIZE)
    setMessages((prev) => [...[...olderDesc].reverse(), ...prev])
    setHasMore(olderDesc.length === PAGE_SIZE)
  }, [chatId, messages])

  const send = useCallback(async (text: string) => {
    const sent = await api.sendMessage(chatId, text)
    setMessages((prev) => (prev.some((m) => m.id === sent.id) ? prev : [...prev, sent]))
  }, [chatId])

  const edit = useCallback(async (id: number, text: string) => {
    const updated = await api.editMessage(chatId, id, text)
    overrides.current.set(id, { kind: 'edited', message: updated, at: Date.now() })
    setMessages((prev) => prev.map((m) => (m.id === id ? updated : m)))
  }, [chatId])

  const remove = useCallback(async (id: number) => {
    await api.deleteMessage(chatId, id)
    overrides.current.set(id, { kind: 'deleted', at: Date.now() })
    setMessages((prev) => prev.filter((m) => m.id !== id))
  }, [chatId])

  return { messages, loading, error, hasMore, loadOlder, send, edit, remove }
}
