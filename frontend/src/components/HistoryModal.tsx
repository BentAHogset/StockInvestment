import { useEffect, useState } from 'react'
import { getHistory, type HistoryPoint } from '../api'

type HistoryModalProps = {
  symbol: string
  onClose: () => void
}

const VIEW_WIDTH = 600
const VIEW_HEIGHT = 200
const PADDING = 12

function buildPolylinePoints(points: HistoryPoint[]): string {
  const closes = points.map(point => point.close)
  const min = Math.min(...closes)
  const max = Math.max(...closes)
  const range = max - min || 1

  return points
    .map((point, index) => {
      const x = PADDING + (index / Math.max(points.length - 1, 1)) * (VIEW_WIDTH - PADDING * 2)
      const y = VIEW_HEIGHT - PADDING - ((point.close - min) / range) * (VIEW_HEIGHT - PADDING * 2)
      return `${x.toFixed(1)},${y.toFixed(1)}`
    })
    .join(' ')
}

export default function HistoryModal({ symbol, onClose }: HistoryModalProps) {
  const [points, setPoints] = useState<HistoryPoint[] | null>(null)
  const [oneYearReturn, setOneYearReturn] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setPoints(null)
    setError(null)

    getHistory(symbol)
      .then(history => {
        if (cancelled) return
        setPoints(history.points)
        setOneYearReturn(history.oneYearReturnPercent)
      })
      .catch(() => {
        if (!cancelled) setError('Kunne ikke hente historiske kurser for denne aksjen.')
      })

    return () => {
      cancelled = true
    }
  }, [symbol])

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-panel" onClick={event => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <strong>{symbol}</strong>
            <span className="muted"> · 1 år tilbake</span>
          </div>
          {oneYearReturn !== null && (
            <span className={oneYearReturn >= 0 ? 'positive' : 'negative'}>
              {oneYearReturn > 0 ? '+' : ''}{oneYearReturn.toFixed(2)}% siste år
            </span>
          )}
          <button className="modal-close" onClick={onClose} aria-label="Lukk">✕</button>
        </div>

        <div className="modal-body">
          {error && <p className="error">{error}</p>}
          {!error && !points && <p className="loading">Laster historikk…</p>}
          {!error && points && points.length > 1 && (
            <svg className="history-chart" viewBox={`0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`} preserveAspectRatio="none">
              <polyline points={buildPolylinePoints(points)} fill="none" stroke="var(--orange)" strokeWidth="2" />
            </svg>
          )}
          {!error && points && points.length <= 1 && <p className="loading">Ingen historikk tilgjengelig.</p>}
        </div>
      </div>
    </div>
  )
}
