import { useState } from 'react'
import { api } from '../api/client'
import type { Chat, User } from '../api/types'
import { fullName } from '../lib/format'
import { ErrorText, ghostButton, inputClass, Modal, primaryButton } from './ui'
import { UserSearch } from './UserSearch'

export function NewChatModal({ onClose, onCreated }: { onClose: () => void; onCreated: (chat: Chat) => void }) {
  const [tab, setTab] = useState<'private' | 'group'>('private')
  const [name, setName] = useState('')
  const [picked, setPicked] = useState<User[]>([])
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function run(action: () => Promise<Chat>) {
    setError('')
    setBusy(true)
    try {
      onCreated(await action())
    } catch (e) {
      setError((e as Error).message)
      setBusy(false)
    }
  }

  const tabClass = (active: boolean) =>
    `flex-1 rounded-lg px-3 py-1.5 text-sm transition ${active ? 'bg-ink text-porcelain' : 'text-porcelain/60 hover:text-porcelain'}`

  return (
    <Modal title="Новый чат" onClose={onClose}>
      <div className="flex gap-1 rounded-xl bg-yale p-1 ring-1 ring-porcelain/10">
        <button className={tabClass(tab === 'private')} onClick={() => setTab('private')}>Личный</button>
        <button className={tabClass(tab === 'group')} onClick={() => setTab('group')}>Группа</button>
      </div>

      {tab === 'private' ? (
        <UserSearch onPick={(u) => run(() => api.createPrivateChat(u.id))} />
      ) : (
        <>
          <input className={inputClass} placeholder="Название группы" value={name}
            onChange={(e) => setName(e.target.value)} maxLength={100} />

          {picked.length > 0 && (
            <ul className="flex flex-wrap gap-2">
              {picked.map((u) => (
                <li key={u.id} className="flex items-center gap-1 rounded-full bg-ink py-1 pl-3 pr-1 text-sm">
                  {fullName(u)}
                  <button className={ghostButton + ' !px-2 !py-0'} aria-label="Убрать"
                    onClick={() => setPicked((p) => p.filter((x) => x.id !== u.id))}>✕</button>
                </li>
              ))}
            </ul>
          )}

          <UserSearch excludeIds={picked.map((u) => u.id)} onPick={(u) => setPicked((p) => [...p, u])} />

          <button className={primaryButton} disabled={busy || !name.trim()}
            onClick={() => run(() => api.createGroupChat(name.trim(), picked.map((u) => u.id)))}>
            Создать группу
          </button>
        </>
      )}

      <ErrorText>{error}</ErrorText>
    </Modal>
  )
}
