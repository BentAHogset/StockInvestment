import { useEffect, useState } from 'react'
import { getPlan, type PlanResponse } from './api'
import OverviewPage from './pages/OverviewPage'
import SimulatorPage from './pages/SimulatorPage'
import ScenarioBuilderPage from './pages/ScenarioBuilderPage'
import './App.css'

type Page = 'overview' | 'simulate' | 'build'

function App() {
  const [page, setPage] = useState<Page>(
    window.location.pathname === '/build' ? 'build' : window.location.pathname === '/simulate' ? 'simulate' : 'overview',
  )
  const [data, setData] = useState<PlanResponse | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getPlan().then(setData).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false))
  }, [])

  const navigate = (nextPage: Page) => {
    const path = nextPage === 'simulate' ? '/simulate' : nextPage === 'build' ? '/build' : '/'
    window.history.pushState({}, '', path)
    setPage(nextPage)
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <button className="brand" onClick={() => navigate('overview')}><span className="brand-mark">CI</span><span>Core Investment</span></button>
        <nav><button className={page === 'overview' ? 'active' : ''} onClick={() => navigate('overview')}>Overview</button><button className={page === 'simulate' || page === 'build' ? 'active' : ''} onClick={() => navigate('simulate')}>Scenario</button></nav>
        <span className="system-state"><i /> API online</span>
      </header>
      {loading && <div className="loading">Loading market intelligence...</div>}
      {error && <div className="error">{error}</div>}
      {!loading && !error && data && page === 'overview' && <OverviewPage data={data} onSimulate={() => navigate('simulate')} />}
      {!loading && !error && data && page === 'simulate' && <SimulatorPage data={data} onBuildScenario={() => navigate('build')} />}
      {!loading && !error && data && page === 'build' && <ScenarioBuilderPage data={data} />}
    </main>
  )
}

export default App
