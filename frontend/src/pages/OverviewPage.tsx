import { money, percent } from '../calculations/investmentCalculations'
import type { PlanResponse } from '../api'
import Metric from '../components/Metric'

type OverviewPageProps = {
  data: PlanResponse
  onSimulate: () => void
}

export default function OverviewPage({ data, onSimulate }: OverviewPageProps) {
  const { plan } = data

  return (
    <>
      <section className="intro">
        <div>
          <p className="eyebrow">MARKET BRIEF / {data.input.strategy.toUpperCase()}</p>
          <h1>Find your next<br /><em>conviction.</em></h1>
          <p className="lede">A focused shortlist of high-upside opportunities, ranked with live market signals and a disciplined allocation plan.</p>
        </div>
        <button className="primary-action" onClick={onSimulate}>Open simulator <span>↗</span></button>
      </section>

      <section className="metrics">
        <Metric label="Monthly allocation" value={money.format(plan.monthlyBudget)} />
        <Metric label="Opportunities" value={String(plan.stockCount).padStart(2, '0')} />
        <Metric label="Data coverage" value={plan.liveDataAvailable ? 'LIVE' : 'FALLBACK'} accent={plan.liveDataAvailable} />
      </section>

      <section className="content-grid">
        <div className="table-section">
          <div className="section-heading">
            <div><p className="eyebrow">01 / RANKED LIST</p><h2>Opportunities</h2></div>
            <span className="live-badge"><i /> {plan.liveDataAvailable ? 'Live quotes loaded' : 'Fallback model'}</span>
          </div>
          <div className="table-wrap">
            <table>
              <thead><tr><th>#</th><th>Asset</th><th>Score</th><th>Last price</th><th>1M move</th><th>Monthly</th></tr></thead>
              <tbody>
                {plan.topStocks.map((stock, index) => {
                  const allocation = plan.allocation.find(item => item.symbol === stock.symbol)
                  const changeClass = stock.dailyChangePercent !== null && stock.dailyChangePercent >= 0 ? 'positive' : 'negative'
                  const change = stock.dailyChangePercent === null ? '—' : `${stock.dailyChangePercent > 0 ? '+' : ''}${percent.format(stock.dailyChangePercent)}%`

                  return (
                    <tr key={stock.symbol}>
                      <td className="muted">{String(index + 1).padStart(2, '0')}</td>
                      <td><strong>{stock.symbol}</strong><small>{stock.reason}</small></td>
                      <td><b className="score">{stock.reliabilityScore}</b></td>
                      <td>{stock.lastPrice === null ? '—' : money.format(stock.lastPrice)}</td>
                      <td className={changeClass}>{change}</td>
                      <td>{allocation ? money.format(allocation.monthlyAmount) : '—'}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>

        <aside className="guidance">
          <p className="eyebrow">02 / FIELD NOTE</p>
          <h3>Stay systematic.</h3>
          <p>{plan.guidance}</p>
          <div className="allocation-bar">{plan.allocation.slice(0, 5).map((item, index) => <span key={item.symbol} style={{ width: `${item.weightPercent}%` }} className={`bar-${index}`} />)}</div>
          <div className="allocation-label"><span>Portfolio weighting</span><b>{plan.allocation.length} assets</b></div>
        </aside>
      </section>
    </>
  )
}
