export type Stock = {
  symbol: string
  reliabilityScore: number
  lastPrice: number | null
  dailyChangePercent: number | null
  usesLiveQuote: boolean
  reason: string
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
  const response = await fetch(`/api/plan?topCount=10&strategy=${strategy}`)
  if (!response.ok) throw new Error('Kunne ikke hente investeringsplanen.')
  return response.json() as Promise<PlanResponse>
}
