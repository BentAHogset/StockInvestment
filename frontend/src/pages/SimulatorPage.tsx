import { type PlanResponse } from '../api'

type SimulatorPageProps = {
  data: PlanResponse
  onBuildScenario: () => void
}

export default function SimulatorPage({ onBuildScenario }: SimulatorPageProps) {
  return (
    <section className="simulator">
      <div className="section-heading">
        <div><p className="eyebrow">INVESTMENT LAB / 03</p><h1>Your scenarios.</h1></div>
        <p className="lede compact">Saved scenarios will show up here once you build and save one.</p>
      </div>

      <p className="loading">No scenarios saved yet.</p>

      <div className="simulator-actions">
        <button className="primary-action" onClick={onBuildScenario}>
          Build scenario <span>↗</span>
        </button>
      </div>
    </section>
  )
}
