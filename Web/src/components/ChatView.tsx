import { useEffect, useLayoutEffect, useRef, useState, type KeyboardEvent } from 'react'
import type { Chat, ChatDetails, Message } from '../api/types'
import { useMessages } from '../hooks/useMessages'
import { chatTitle, formatDay, formatTime, fullName, sameDay } from '../lib/format'
import { Avatar, ErrorText, ghostButton, primaryButton } from './ui'

const NEAR_BOTTOM_PX = 120

interface Props {
  chat: Chat
  details?: ChatDetails
  meId: number
  onOpenMembers: () => void
}

export function ChatView({ chat, details, meId, onOpenMembers }: Props) {
  const { messages, loading, error, hasMore, loadOlder, send, edit, remove } = useMessages(chat.id)
  const [draft, setDraft] = useState('')
  const [editing, setEditing] = useState<Message | null>(null)
  const [actionError, setActionError] = useState('')
  const scroller = useRef<HTMLDivElement>(null)
  const stickToBottom = useRef(true)
  const sending = useRef(false)
  const input = useRef<HTMLTextAreaElement>(null)

  const title = chatTitle(chat, details, meId)
  const isGroup = chat.type === 'Group'
  const names = new Map(details?.members.map((m) => [m.userId, fullName(m)]))

  // Новый чат — к последнему сообщению.
  useEffect(() => { stickToBottom.current = true; setDraft(''); setEditing(null) }, [chat.id])

  useLayoutEffect(() => {
    const el = scroller.current
    if (el && stickToBottom.current) el.scrollTop = el.scrollHeight
  }, [messages])

  const onScroll = () => {
    const el = scroller.current
    if (el) stickToBottom.current = el.scrollHeight - el.scrollTop - el.clientHeight < NEAR_BOTTOM_PX
  }

  async function submit() {
    const text = draft.trim()
    if (!text || sending.current) return
    sending.current = true
    const wasEditing = editing

    // Очищаем поле сразу, чтобы набор следующего сообщения не смешивался с отправляемым.
    setActionError('')
    setDraft('')
    setEditing(null)
    try {
      if (wasEditing) await edit(wasEditing.id, text)
      else { stickToBottom.current = true; await send(text) }
    } catch (e) {
      setDraft(text)
      setEditing(wasEditing)
      setActionError((e as Error).message)
    } finally {
      sending.current = false
    }
  }

  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); void submit() }
    if (e.key === 'Escape') { setEditing(null); setDraft('') }
  }

  function startEdit(m: Message) {
    setEditing(m)
    setDraft(m.text)
    // Фокус в поле и курсор в конец текста.
    requestAnimationFrame(() => {
      const el = input.current
      el?.focus()
      el?.setSelectionRange(m.text.length, m.text.length)
    })
  }

  async function onDelete(m: Message) {
    setActionError('')
    try { await remove(m.id) } catch (e) { setActionError((e as Error).message) }
  }

  return (
    <section className="flex h-full min-w-0 flex-1 flex-col">
      <header className="flex items-center gap-3 bg-yale px-4 py-3">
        <Avatar name={title} />
        <div className="min-w-0 flex-1">
          <h2 className="truncate font-semibold">{title}</h2>
          {isGroup && details && <p className="text-xs text-porcelain/60">{details.members.length} участн.</p>}
        </div>
        {isGroup && <button className={ghostButton} onClick={onOpenMembers}>Участники</button>}
      </header>

      <div ref={scroller} onScroll={onScroll} className="flex flex-1 flex-col gap-1 overflow-y-auto px-4 py-4">
        {hasMore && (
          <button className={ghostButton + ' mx-auto mb-2'} onClick={() => void loadOlder()}>
            Показать более ранние
          </button>
        )}
        {loading && <p className="m-auto text-porcelain/60">Загрузка…</p>}
        {!loading && messages.length === 0 && !error && (
          <p className="m-auto text-porcelain/60">Сообщений пока нет. Напишите первым!</p>
        )}
        <ErrorText>{error}</ErrorText>

        {messages.map((m, i) => {
          const mine = m.senderId === meId
          const newDay = i === 0 || !sameDay(messages[i - 1]!.createdAt, m.createdAt)
          return (
            <div key={m.id} className="flex flex-col">
              {newDay && (
                <div className="my-3 self-center rounded-full bg-yale px-3 py-0.5 text-xs text-porcelain/70">
                  {formatDay(m.createdAt)}
                </div>
              )}
              <div className={`group flex items-end gap-2 ${mine ? 'flex-row-reverse' : ''}`}>
                <div className={`max-w-[75%] rounded-2xl px-3 py-2 ${mine ? 'rounded-br-sm bg-blush text-ink' : 'rounded-bl-sm bg-yale'}`}>
                  {isGroup && !mine && <p className="text-xs font-medium text-sage">{names.get(m.senderId) ?? 'Участник'}</p>}
                  <p className="whitespace-pre-wrap break-words">{m.text}</p>
                  <p className={`mt-0.5 text-right text-[11px] ${mine ? 'text-ink/60' : 'text-porcelain/50'}`}>
                    {m.editedAt && 'изменено · '}{formatTime(m.createdAt)}
                  </p>
                </div>
                {mine && (
                  <div className="flex gap-1 opacity-0 transition group-hover:opacity-100">
                    <button className={ghostButton} onClick={() => startEdit(m)}>Изменить</button>
                    <button className={ghostButton} onClick={() => void onDelete(m)}>Удалить</button>
                  </div>
                )}
              </div>
            </div>
          )
        })}
      </div>

      <footer className="flex flex-col gap-2 bg-yale px-4 py-3">
        {editing && (
          <div className="flex items-center justify-between text-sm text-sage">
            <span>Редактирование сообщения</span>
            <button className={ghostButton} onClick={() => { setEditing(null); setDraft('') }}>Отмена</button>
          </div>
        )}
        <ErrorText>{actionError}</ErrorText>
        <div className="flex items-end gap-2">
          <textarea
            ref={input}
            className="max-h-32 min-h-10 flex-1 resize-none rounded-lg bg-ink px-3 py-2 outline-none ring-1 ring-porcelain/10 placeholder:text-porcelain/40 focus:ring-2 focus:ring-sage"
            rows={1}
            placeholder="Сообщение…"
            value={draft}
            maxLength={4000}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={onKeyDown}
          />
          <button className={primaryButton} disabled={!draft.trim()} onClick={() => void submit()}>
            {editing ? 'Сохранить' : 'Отправить'}
          </button>
        </div>
      </footer>
    </section>
  )
}
