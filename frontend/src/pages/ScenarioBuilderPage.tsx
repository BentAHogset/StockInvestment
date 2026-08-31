import { useState } from 'react'
import { createScenario, type PlanResponse } from '../api'
import { futureValue, money } from '../calculations/investmentCalculations'
import Metric from '../components/Metric'
import HistoryModal from '../components/HistoryModal'

type ScenarioBuilderPageProps = {
  data: PlanResponse
  onScenarioSaved: () => void
}

type SimulationRow = {
  symbol: string
  monthly: number
  annualReturn: number
  invested: number
  projected: number
  profit: number
}

export default function ScenarioBuilderPage({ data, onScenarioSaved }: ScenarioBuilderPageProps) {
  const [years, setYears] = useState(10)
  const [name, setName] = useState('My scenario')
  const [amounts, setAmounts] = useState<Record<string, number>>(
    () => Object.fromEntries(data.plan.topStocks.map(stock => [stock.symbol, 50])),
  )
  const [openSymbol, setOpenSymbol] = useState<string | null>(null)
  const [saveState, setSaveState] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle')

  const rows: SimulationRow[] = data.plan.topStocks.map(stock => {
    const monthly = amounts[stock.symbol] || 0
    const rate = stock.estimatedAnnualReturnPercent ?? 0
    const invested = monthly * 12 * years
    const projected = futureValue(monthly, rate, years)

    return { symbol: stock.symbol, monthly, annualReturn: rate, invested, projected, profit: projected - invested }
  })

  const totals = rows.reduce(
    (sum, row) => ({
      monthly: sum.monthly + row.monthly,
      invested: sum.invested + row.invested,
      projected: sum.projected + row.projected,
      profit: sum.profit + row.profit,
    }),
    { monthly: 0, invested: 0, projected: 0, profit: 0 },
  )

  return (
    <section className="simulator">
      <div className="section-heading">
        <div><p className="eyebrow">INVESTMENT LAB / 03</p><h1>Build a scenario.</h1></div>
        <p className="lede compact">Adjust the inputs and see what steady contributions could become over time.</p>
      </div>

      <div className="controls">
        <label>Name
          <input value={name} maxLength={100} onChange={event => setName(event.target.value)} />
        </label>
        <label>Horizon
          <select value={years} onChange={event => setYears(Number(event.target.value))}>
            {Array.from({ length: 20 }, (_, index) => <option key={index + 1} value={index + 1}>{index + 1} years</option>)}
          </select>
        </label>
      </div>

      <div className="metrics simulator-metrics">
        <Metric label="Total monthly" value={money.format(totals.monthly)} />
        <Metric label="Total invested" value={money.format(totals.invested)} />
        <Metric label="Projected value" value={money.format(totals.projected)} accent />
        <Metric label="Estimated profit" value={money.format(totals.profit)} accent={totals.profit >= 0} negative={totals.profit < 0} />
      </div>

      <div className="table-wrap">
        <table>
          <thead><tr><th>Asset</th><th>Monthly</th><th>Invested</th><th>Projected value</th><th>Estimated profit</th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.symbol}>
                <td><button className="symbol-link" onClick={() => setOpenSymbol(row.symbol)}>{row.symbol}</button></td>
                <td><input className="amount-input" type="number" min="0" step="1" value={row.monthly} onChange={event => setAmounts({ ...amounts, [row.symbol]: Math.max(0, Number(event.target.value)) })} /></td>
                <td>{money.format(row.invested)}</td>
                <td>{money.format(row.projected)}</td>
                <td className={row.profit >= 0 ? 'positive' : 'negative'}>{money.format(row.profit)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="simulator-actions">
        <button
          className="primary-action"
          disabled={saveState === 'saving'}
          onClick={() => {
            setSaveState('saving')
            createScenario(name, years, rows.map(row => ({ ticker: row.symbol, investedAmount: row.invested, valueAmount: row.projected })))
              .then(() => {
                setSaveState('saved')
                onScenarioSaved()
              })
              .catch(() => setSaveState('error'))
          }}
        >
          {saveState === 'saving' ? 'Saving...' : saveState === 'saved' ? 'Saved' : 'Save scenario'} <span>↗</span>
        </button>
        {saveState === 'error' && <span className="error">Could not save scenario.</span>}
      </div>

      {openSymbol && <HistoryModal symbol={openSymbol} onClose={() => setOpenSymbol(null)} />}
    </section>
  )
}
