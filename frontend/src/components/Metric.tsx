type MetricProps = {
  label: string
  value: string
  accent?: boolean
  negative?: boolean
}

export default function Metric({ label, value, accent = false, negative = false }: MetricProps) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong className={negative ? 'negative' : accent ? 'accent' : ''}>{value}</strong>
    </div>
  )
}
