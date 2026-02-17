async function main() {
  const res = await fetch("./history.json", { cache: "no-store" });
  const data = await res.json();
  const points = (data.points || []).slice().reverse();

  const labels = points.map(p => p.date);
  const line = points.map(p => p.linePct);
  const branch = points.map(p => p.branchPct);

  const ctx = document.getElementById("chart");
  new Chart(ctx, {
    type: "line",
    data: {
      labels,
      datasets: [
        { label: "Line %", data: line, spanGaps: true },
        { label: "Branch %", data: branch, spanGaps: true }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: { y: { min: 0, max: 100 } },
      plugins: { legend: { position: "top" } }
    }
  });

  const tbody = document.querySelector("#runs tbody");
  for (const p of points.slice(0, 25)) {
    const tr = document.createElement("tr");
    const shaShort = (p.sha || "").slice(0, 8);
    tr.innerHTML = `
      <td>${p.date || ""}</td>
      <td>${p.linePct ?? ""}</td>
      <td>${p.branchPct ?? ""}</td>
      <td><code title="${p.sha || ""}">${shaShort}</code></td>
    `;
    tbody.appendChild(tr);
  }
}

main().catch(err => {
  document.body.insertAdjacentHTML("beforeend", "<p class='muted'>Failed to load history.json</p>");
  console.error(err);
});
