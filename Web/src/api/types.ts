export type ChatType = 'Private' | 'Group'
export type MemberRole = 'Owner' | 'Admin' | 'Member'

export interface User {
  id: number
  firstName: string
  lastName: string
  avatarUrl: string | null
}

export interface Me extends User {
  email: string
  createdAt: string
}

export interface AuthResponse {
  token: string
  expiresAt: string
  user: Me
}

export interface Chat {
  id: number
  name: string | null
  avatarUrl: string | null
  type: ChatType
  createdBy: number
  createdAt: string
}

export interface Member {
  userId: number
  firstName: string
  lastName: string
  avatarUrl: string | null
  role: MemberRole
  joinedAt: string
}

export interface ChatDetails extends Chat {
  members: Member[]
}

export interface Message {
  id: number
  chatId: number
  senderId: number
  text: string
  createdAt: string
  editedAt: string | null
}
