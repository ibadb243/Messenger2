import { useEffect, type ReactNode } from 'react'
import { initials } from '../lib/format'

// Общие стили. Распределение цветов: ink — фон, yale — поверхности, blush — главные действия,
// porcelain — текст, sage — мелкие акценты (фокус, бейджи).
export const inputClass =
  'w-full rounded-lg bg-ink px-3 py-2 text-porcelain placeholder:text-porcelain/40 ' +
  'outline-none ring-1 ring-porcelain/10 focus:ring-2 focus:ring-sage'

export const primaryButton =
  'rounded-lg bg-blush px-4 py-2 font-medium text-ink transition hover:brightness-110 ' +
  'disabled:cursor-not-allowed disabled:opacity-40'

export const ghostButton =
  'rounded-lg px-3 py-1.5 text-sm text-porcelain/80 transition hover:bg-ink/40 hover:text-porcelain ' +
  'disabled:cursor-not-allowed disabled:opacity-40'

export function Avatar({ name, size = 40 }: { name: string; size?: number }) {
  return (
    <div
      className="flex shrink-0 items-center justify-center rounded-full bg-ink font-semibold text-blush"
      style={{ width: size, height: size, fontSize: size * 0.38 }}
      aria-hidden
    >
      {initials(name)}
    </div>
  )
}

export function Badge({ children }: { children: ReactNode }) {
  return <span className="rounded-full bg-sage px-2 py-0.5 text-[11px] font-medium text-ink">{children}</span>
}

export function ErrorText({ children }: { children?: ReactNode }) {
  return children ? <p className="rounded-lg bg-ink px-3 py-2 text-sm text-blush" role="alert">{children}</p> : null
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-ink/80 p-4" onMouseDown={onClose}>
      <div
        className="flex max-h-[85vh] w-full max-w-md flex-col gap-4 overflow-y-auto rounded-2xl bg-yale p-5 shadow-xl"
        onMouseDown={(e) => e.stopPropagation()}
        role="dialog"
        aria-label={title}
      >
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">{title}</h2>
          <button className={ghostButton} onClick={onClose} aria-label="Закрыть">✕</button>
        </div>
        {children}
      </div>
    </div>
  )
}
