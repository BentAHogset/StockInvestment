type MetricProps = {
  label: string
  value: string
  accent?: boolean
}

export default function Metric({ label, value, accent = false }: MetricProps) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong className={accent ? 'accent' : ''}>{value}</strong>
    </div>
  )
}
