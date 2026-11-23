import { useEffect, useState } from 'react'
import { getPlan, type PlanResponse } from './api'
import OverviewPage from './pages/OverviewPage'
import SimulatorPage from './pages/SimulatorPage'
import ScenarioBuilderPage from './pages/ScenarioBuilderPage'
import './App.css'

type Page = 'overview' | 'simulate' | 'build'

function readRoute(): { page: Page; scenarioId: number | null } {
  const path = window.location.pathname
  const buildMatch = /^\/build(?:\/([^/]+))?$/.exec(path)

  if (buildMatch) {
    const parsedId = Number(buildMatch[1])
    return {
      page: 'build',
      scenarioId: buildMatch[1] && Number.isInteger(parsedId) && parsedId > 0 ? parsedId : null,
    }
  }

  if (path === '/simulate') return { page: 'simulate', scenarioId: null }
  return { page: 'overview', scenarioId: null }
}

function App() {
  const initialRoute = readRoute()
  const [page, setPage] = useState<Page>(initialRoute.page)
  const [scenarioId, setScenarioId] = useState<number | null>(initialRoute.scenarioId)
  const [data, setData] = useState<PlanResponse | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [scenarioRefreshVersion, setScenarioRefreshVersion] = useState(0)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError('')

    getPlan()
      .then((result) => {
        if (!cancelled) setData(result)
      })
      .catch((reason: Error) => {
        if (!cancelled) setError(reason.message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    const handlePopState = () => {
      const route = readRoute()
      setPage(route.page)
      setScenarioId(route.scenarioId)
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const navigate = (nextPage: Page, nextScenarioId: number | null = null) => {
    const path =
      nextPage === 'simulate'
        ? '/simulate'
        : nextPage === 'build'
        ? nextScenarioId
          ? `/build/${nextScenarioId}`
          : '/build'
        : '/'
    window.history.pushState({}, '', path)
    setPage(nextPage)
    setScenarioId(nextScenarioId)
  }

  const handleScenarioSaved = () => {
    setScenarioRefreshVersion((version) => version + 1)
    navigate('simulate')
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <button className="brand" onClick={() => navigate('overview')}>
          <span className="brand-mark">CI</span>
          <span>Core Investment</span>
        </button>
        <nav>
          <button className={page === 'overview' ? 'active' : ''} onClick={() => navigate('overview')}>
            Overview
          </button>
          <button
            className={page === 'simulate' || page === 'build' ? 'active' : ''}
            onClick={() => navigate('simulate')}
          >
            Scenario
          </button>
        </nav>
        <span className="system-state">
          <i /> API online
        </span>
      </header>
      {loading && <div className="loading">Loading market intelligence...</div>}
      {error && <div className="error">{error}</div>}
      {!loading && !error && data && page === 'overview' && (
        <OverviewPage data={data} onSimulate={() => navigate('simulate')} />
      )}
      {!loading && !error && data && page === 'simulate' && (
        <SimulatorPage
          data={data}
          onBuildScenario={() => navigate('build')}
          onOpenScenario={(id) => navigate('build', id)}
          refreshVersion={scenarioRefreshVersion}
        />
      )}
      {!loading && !error && data && page === 'build' && (
        <ScenarioBuilderPage data={data} scenarioId={scenarioId} onScenarioSaved={handleScenarioSaved} />
      )}
    </main>
  )
}

export default App
