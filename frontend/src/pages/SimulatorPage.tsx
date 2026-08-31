import { useEffect, useState } from 'react'
import { getScenarios, type PlanResponse, type Scenario } from '../api'

type SimulatorPageProps = {
  data: PlanResponse
  onBuildScenario: () => void
  refreshVersion: number
}

export default function SimulatorPage({ onBuildScenario, refreshVersion }: SimulatorPageProps) {
  const [scenarios, setScenarios] = useState<Scenario[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    const fetchScenarios = async () => {
      setLoading(true)
      try {
        const data = await getScenarios()
        if (!cancelled) {
          setScenarios(data)
          setError('')
        }
      } catch (reason) {
        if (!cancelled) {
          setError((reason as Error).message)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    fetchScenarios()

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        fetchScenarios()
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      cancelled = true
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [refreshVersion])

  return (
    <section className="simulator">
      <div className="section-heading">
        <div><p className="eyebrow">INVESTMENT LAB / 03</p><h1>Your scenarios.</h1></div>
        <p className="lede compact">Saved scenarios will show up here once you build and save one.</p>
      </div>

      {loading && <p className="loading">Loading saved scenarios...</p>}
      {error && <p className="error">{error}</p>}
      {!loading && !error && scenarios.length === 0 && <p className="loading">No scenarios saved yet.</p>}
      {!loading && !error && scenarios.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Name</th><th>Horizon</th><th>Assets</th><th>Updated</th></tr></thead>
            <tbody>
              {scenarios.map(scenario => (
                <tr key={scenario.id}>
                  <td><strong>{scenario.name}</strong></td>
                  <td>{scenario.horizon === null ? '—' : `${scenario.horizon} years`}</td>
                  <td>{scenario.assets.length}</td>
                  <td>{scenario.created ? new Date(scenario.created).toLocaleDateString() : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="simulator-actions">
        <button className="primary-action" onClick={onBuildScenario}>
          Build scenario <span>↗</span>
        </button>
      </div>
    </section>
  )
}
