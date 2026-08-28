{{/*
Common labels applied to every resource this chart creates.
*/}}
{{- define "payflow.labels" -}}
app.kubernetes.io/part-of: payflow
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{/*
Labels that select a specific component's pods (used on both the Deployment/StatefulSet's
selector and its pod template — must stay identical, hence factored out once).
*/}}
{{- define "payflow.selectorLabels" -}}
app.kubernetes.io/name: {{ .name }}
{{- end -}}
