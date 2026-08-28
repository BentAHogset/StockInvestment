export const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
export const percent = new Intl.NumberFormat('en-US', { maximumFractionDigits: 2, minimumFractionDigits: 2 })

export function futureValue(monthly: number, annualRate: number, years: number) {
  const rate = annualRate / 100 / 12
  const periods = years * 12
  return rate === 0 ? monthly * periods : monthly * ((Math.pow(1 + rate, periods) - 1) / rate)
}
