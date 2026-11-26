# Messenger2 Web

React + TypeScript + Vite + Tailwind CSS. Интерфейс на русском.

## Запуск

Через Docker (из корня репозитория) — фронтенд будет на http://localhost:3000, `/api` проксирует nginx:

```powershell
docker compose up -d --build
```

Для разработки (Api должен быть доступен на http://localhost:8080):

```powershell
npm install
npm run dev
```

Другой адрес Api для dev-режима: переменная `VITE_API_PROXY`, например `http://localhost:5097`.

## Устройство

- `src/api` — типы и клиент Api (JWT хранится в `localStorage`).
- `src/auth` — контекст авторизации.
- `src/hooks/useMessages.ts` — история, опрос новых сообщений, отправка / правка / удаление.
- `src/components` — экраны и модальные окна.
- Палитра (правило 60-30-10) описана в `src/index.css`.
