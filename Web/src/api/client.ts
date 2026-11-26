import type {
  AuthResponse, Chat, ChatDetails, Me, Member, MemberRole, Message, User,
} from './types'

const TOKEN_KEY = 'messenger.token'

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

/** Ошибка API: message берётся из ProblemDetails.detail (на русском, как отдаёт бэкенд). */
export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

let onUnauthorized: () => void = () => {}
export const setUnauthorizedHandler = (handler: () => void) => { onUnauthorized = handler }

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const token = tokenStore.get()
  const response = await fetch(`/api${path}`, {
    method,
    headers: {
      ...(body !== undefined && { 'Content-Type': 'application/json' }),
      ...(token && { Authorization: `Bearer ${token}` }),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (response.status === 401 && token) onUnauthorized()
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? 'Что-то пошло не так. Попробуйте ещё раз.')
  }
  return response.status === 204 ? (undefined as T) : response.json()
}

export const api = {
  login: (email: string, password: string) =>
    request<AuthResponse>('POST', '/auth/login', { email, password }),
  register: (email: string, password: string, firstName: string, lastName: string) =>
    request<AuthResponse>('POST', '/auth/register', { email, password, firstName, lastName }),

  me: () => request<Me>('GET', '/users/me'),
  searchUsers: (q: string) => request<User[]>('GET', `/users/search?q=${encodeURIComponent(q)}`),

  chats: () => request<Chat[]>('GET', '/chats'),
  chat: (id: number) => request<ChatDetails>('GET', `/chats/${id}`),
  createPrivateChat: (userId: number) => request<Chat>('POST', '/chats/private', { userId }),
  createGroupChat: (name: string, memberIds: number[]) =>
    request<Chat>('POST', '/chats/group', { name, memberIds }),
  addMember: (chatId: number, userId: number, role: MemberRole = 'Member') =>
    request<Member>('POST', `/chats/${chatId}/members`, { userId, role }),
  removeMember: (chatId: number, userId: number) =>
    request<void>('DELETE', `/chats/${chatId}/members/${userId}`),

  messages: (chatId: number, before?: number, limit?: number) => {
    const params = new URLSearchParams()
    if (before) params.set('before', String(before))
    if (limit) params.set('limit', String(limit))
    return request<Message[]>('GET', `/chats/${chatId}/messages?${params}`)
  },
  sendMessage: (chatId: number, text: string) =>
    request<Message>('POST', `/chats/${chatId}/messages`, { text }),
  editMessage: (chatId: number, id: number, text: string) =>
    request<Message>('PUT', `/chats/${chatId}/messages/${id}`, { text }),
  deleteMessage: (chatId: number, id: number) =>
    request<void>('DELETE', `/chats/${chatId}/messages/${id}`),
}
