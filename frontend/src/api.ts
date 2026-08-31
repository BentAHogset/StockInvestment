export type Stock = {
  symbol: string
  reliabilityScore: number
  lastPrice: number | null
  dailyChangePercent: number | null
  usesLiveQuote: boolean
  reason: string
  targetMeanPrice: number | null
  estimatedAnnualReturnPercent: number | null
}

export type Allocation = {
  symbol: string
  weightPercent: number
  monthlyAmount: number
  strategy: string
}

export type Plan = {
  monthlyBudget: number
  stockCount: number
  liveDataAvailable: boolean
  topStocks: Stock[]
  allocation: Allocation[]
  guidance: string
}

export type PlanResponse = {
  service: string
  input: { topCount: number; strategy: string }
  plan: Plan
}

export async function getPlan(strategy = 'aggressive'): Promise<PlanResponse> {
  const response = await fetch(`/api/plan?topCount=10&strategy=${strategy}`, {
    cache: 'no-store',
  })
  if (!response.ok) throw new Error('Kunne ikke hente investeringsplanen.')
  return response.json() as Promise<PlanResponse>
}

export type HistoryPoint = { date: string; close: number }

export type HistoryResponse = {
  symbol: string
  oneYearReturnPercent: number | null
  points: HistoryPoint[]
}

export async function getHistory(symbol: string): Promise<HistoryResponse> {
  const response = await fetch(`/api/history/${symbol}`, {
    cache: 'no-store',
  })
  if (!response.ok) throw new Error('Kunne ikke hente historikk.')
  return response.json() as Promise<HistoryResponse>
}

export type ScenarioAssetInput = {
  ticker: string
  investedAmount: number
  valueAmount: number
}

export type Scenario = {
  id: number
  name: string
  horizon: number | null
  assets: ScenarioAssetInput[]
  created: string | null
}

export async function createScenario(name: string, years: number, assets: ScenarioAssetInput[]): Promise<Scenario> {
  const response = await fetch('/api/scenarios', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, years, assets }),
  })
  if (!response.ok) throw new Error('Kunne ikke lagre scenarioet.')
  return response.json() as Promise<Scenario>
}

export async function getScenarios(): Promise<Scenario[]> {
  const response = await fetch('/api/scenarios', {
    cache: 'no-store',
  })
  if (!response.ok) throw new Error('Kunne ikke hente scenarioene.')
  return response.json() as Promise<Scenario[]>
}
