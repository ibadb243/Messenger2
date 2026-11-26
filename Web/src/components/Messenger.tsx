import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { Chat, ChatDetails } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { chatTitle, fullName } from '../lib/format'
import { ChatView } from './ChatView'
import { MembersModal } from './MembersModal'
import { NewChatModal } from './NewChatModal'
import { Avatar, Badge, ghostButton, primaryButton } from './ui'

const CHATS_POLL_MS = 5000

export function Messenger() {
  const { me, signOut } = useAuth()
  const [chats, setChats] = useState<Chat[]>([])
  const [details, setDetails] = useState<Record<number, ChatDetails>>({})
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [showNew, setShowNew] = useState(false)
  const [showMembers, setShowMembers] = useState(false)

  const loadDetails = useCallback((chatId: number) =>
    api.chat(chatId).then((d) => setDetails((prev) => ({ ...prev, [chatId]: d }))).catch(() => {}), [])

  // Новые чаты сверху: порядок должен совпадать и после создания, и после обновления списка.
  const byNewest = (list: Chat[]) => [...list].sort((a, b) => b.id - a.id)

  const loadChats = useCallback(() =>
    api.chats().then((list) => setChats(byNewest(list))).catch(() => {}), [])

  useEffect(() => {
    void loadChats()
    const timer = setInterval(() => void loadChats(), CHATS_POLL_MS)
    return () => clearInterval(timer)
  }, [loadChats])

  // Для личных чатов название — имя собеседника, его берём из деталей чата (грузим один раз).
  useEffect(() => {
    chats.filter((c) => !details[c.id]).forEach((c) => void loadDetails(c.id))
  }, [chats, details, loadDetails])

  if (!me) return null
  const selected = chats.find((c) => c.id === selectedId)

  function onCreated(chat: Chat) {
    setShowNew(false)
    setChats((prev) => (prev.some((c) => c.id === chat.id) ? prev : [chat, ...prev]))
    setSelectedId(chat.id)
  }

  return (
    <div className="flex h-full">
      <aside className="flex w-80 shrink-0 flex-col bg-yale">
        <div className="flex items-center gap-3 p-4">
          <Avatar name={fullName(me)} />
          <div className="min-w-0 flex-1">
            <p className="truncate font-semibold">{fullName(me)}</p>
            <p className="truncate text-xs text-porcelain/60">{me.email}</p>
          </div>
          <button className={ghostButton} onClick={signOut}>Выйти</button>
        </div>

        <div className="px-4 pb-3">
          <button className={primaryButton + ' w-full'} onClick={() => setShowNew(true)}>+ Новый чат</button>
        </div>

        <ul className="flex-1 overflow-y-auto">
          {chats.length === 0 && <li className="px-4 py-6 text-center text-sm text-porcelain/60">Чатов пока нет</li>}
          {chats.map((c) => {
            const title = chatTitle(c, details[c.id], me.id)
            const active = c.id === selectedId
            return (
              <li key={c.id}>
                <button
                  onClick={() => setSelectedId(c.id)}
                  className={`flex w-full items-center gap-3 border-l-4 px-4 py-3 text-left transition ${
                    active ? 'border-sage bg-ink/60' : 'border-transparent hover:bg-ink/30'
                  }`}
                >
                  <Avatar name={title} />
                  <span className="min-w-0 flex-1 truncate">{title}</span>
                  {c.type === 'Group' && <Badge>группа</Badge>}
                </button>
              </li>
            )
          })}
        </ul>
      </aside>

      <main className="flex min-w-0 flex-1">
        {selected ? (
          <ChatView key={selected.id} chat={selected} details={details[selected.id]} meId={me.id}
            onOpenMembers={() => setShowMembers(true)} />
        ) : (
          <div className="m-auto text-center text-porcelain/60">
            <p className="text-lg">Выберите чат или создайте новый</p>
          </div>
        )}
      </main>

      {showNew && <NewChatModal onClose={() => setShowNew(false)} onCreated={onCreated} />}

      {showMembers && selected && details[selected.id] && (
        <MembersModal
          chat={details[selected.id]!}
          meId={me.id}
          onClose={() => setShowMembers(false)}
          onChanged={() => void loadDetails(selected.id)}
          onLeft={() => { setShowMembers(false); setSelectedId(null); void loadChats() }}
        />
      )}
    </div>
  )
}
