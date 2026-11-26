import { useState } from 'react'
import { api } from '../api/client'
import type { ChatDetails, Member } from '../api/types'
import { fullName } from '../lib/format'
import { Avatar, Badge, ErrorText, ghostButton, Modal } from './ui'
import { UserSearch } from './UserSearch'

const ROLE_LABEL = { Owner: 'владелец', Admin: 'админ', Member: '' } as const

export function MembersModal({ chat, meId, onClose, onChanged, onLeft }: {
  chat: ChatDetails
  meId: number
  onClose: () => void
  onChanged: () => void
  onLeft: () => void
}) {
  const [error, setError] = useState('')
  const me = chat.members.find((m) => m.userId === meId)
  const canManage = me?.role === 'Owner' || me?.role === 'Admin'

  const canRemove = (m: Member) =>
    m.userId !== meId && (me?.role === 'Owner' || (me?.role === 'Admin' && m.role === 'Member'))

  async function run(action: () => Promise<unknown>, after: () => void) {
    setError('')
    try {
      await action()
      after()
    } catch (e) {
      setError((e as Error).message)
    }
  }

  return (
    <Modal title={chat.name ?? 'Участники'} onClose={onClose}>
      <ul className="flex flex-col">
        {chat.members.map((m) => (
          <li key={m.userId} className="flex items-center gap-3 rounded-lg px-2 py-2">
            <Avatar name={fullName(m)} size={32} />
            <span className="flex-1">{fullName(m)}{m.userId === meId && ' (вы)'}</span>
            {ROLE_LABEL[m.role] && <Badge>{ROLE_LABEL[m.role]}</Badge>}
            {canRemove(m) && (
              <button className={ghostButton} onClick={() => run(() => api.removeMember(chat.id, m.userId), onChanged)}>
                Убрать
              </button>
            )}
          </li>
        ))}
      </ul>

      {canManage && (
        <div className="flex flex-col gap-2">
          <h3 className="text-sm text-porcelain/70">Добавить участника</h3>
          <UserSearch excludeIds={chat.members.map((m) => m.userId)}
            onPick={(u) => run(() => api.addMember(chat.id, u.id), onChanged)} />
        </div>
      )}

      <ErrorText>{error}</ErrorText>

      {me && me.role !== 'Owner' && (
        <button className={ghostButton + ' self-start text-blush'}
          onClick={() => run(() => api.removeMember(chat.id, meId), onLeft)}>
          Покинуть группу
        </button>
      )}
    </Modal>
  )
}
