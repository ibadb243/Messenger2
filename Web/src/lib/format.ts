import type { Chat, ChatDetails, User } from '../api/types'

export const fullName = (u: Pick<User, 'firstName' | 'lastName'>) => `${u.firstName} ${u.lastName}`.trim()

export const initials = (text: string) =>
  text.split(/\s+/).filter(Boolean).slice(0, 2).map((w) => w[0]!.toUpperCase()).join('') || '?'

/** Название чата: у группы — своё имя, у личного — имя собеседника. */
export function chatTitle(chat: Chat, details: ChatDetails | undefined, meId: number): string {
  if (chat.type === 'Group') return chat.name ?? 'Группа'
  const other = details?.members.find((m) => m.userId !== meId)
  return other ? fullName(other) : 'Личный чат'
}

export const formatTime = (iso: string) =>
  new Date(iso).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })

export const formatDay = (iso: string) =>
  new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long' })

export const sameDay = (a: string, b: string) => new Date(a).toDateString() === new Date(b).toDateString()
