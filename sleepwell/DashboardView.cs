namespace SleepWellApp
{
    public static class DashboardView
    {
        public static string GetHtml()
        {
            // @"" for HTML content
            return @"
<html>
<head>
    <title>SleepWell Dashboard</title>
    <script src='https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js'></script>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0b0c10; color: #c5c6c7; margin: 20px; }
        .container { max-width: 1100px; margin: auto; } 
        h1 { color: #66fcf1; margin-bottom: 10px; }
        
        .buttons button { background-color: #45a29e; color: white; border: none; padding: 10px 20px; margin: 5px; cursor: pointer; border-radius: 5px; font-weight: bold; }
        .buttons button:hover { background-color: #388e89; }
        button:disabled { background-color: #555; cursor: not-allowed; }

        .chart-container { position: relative; background-color: #1f2833; padding: 20px; margin-top: 20px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.3); }
        .chart-container.small { height: 400px; }
        .chart-container.large { height: 500px; }
        
        .charts-row { display: flex; gap: 20px; flex-wrap: wrap; }
        .chart-wrapper-bar { flex: 2; min-width: 500px; }
        .chart-wrapper-pie { flex: 1; min-width: 300px; }

        .header-content { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 20px; border-bottom: 1px solid #1f2833; padding-bottom: 20px; }
        .header-text { flex-grow: 1; }
        .header-image { width: 150px; height: auto; margin-left: 20px; opacity: 0.8; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header-content'>
            <div class='header-text'>
                <h1>Fitbit Sleep Data Dashboard</h1>
                <p>Status: <strong>Ready to Sync</strong></p>
                
                <a href='/sync/start' target='_blank' onclick='disableSyncButton(this)'>
                    <button id='btnSync'>Manually Trigger Sync (60 Days)</button>
                </a>

                <h3 style='margin-top: 20px;'>Select Data Range</h3>
                <div class='buttons'>
                    <button onclick='fetchData(7)'>Last 7 Days</button>
                    <button onclick='fetchData(30)'>Last 30 Days</button>
                    <button onclick='fetchData(60)'>Last 60 Days</button>
                </div>
            </div>
            <img src='https://www.ncaa.com/sites/default/files/images/logos/schools/bgd/elmhurst.svg' alt='Bluejay Spirit!' class='header-image'>
        </div>


        <div class='chart-container large'>
             <canvas id='trendGraph'></canvas>
        </div>

        <div class='charts-row'>
            <div class='chart-wrapper-bar'>
                <div class='chart-container small'>
                    <canvas id='sleepGraph'></canvas>
                </div>
            </div>

            <div class='chart-wrapper-pie'>
                <div class='chart-container small'>
                    <canvas id='sleepPieChart'></canvas>
                </div>
            </div>
        </div>

        <div class='chart-container small'>
             <canvas id='durationGraph'></canvas>
        </div>

    </div>
    <script>
        let sleepChart = null;
        let pieChart = null;
        let durationChart = null; 
        let trendChart = null; 
        
        window.onload = function() { fetchData(7); };

        function disableSyncButton(link) {
            const btn = document.getElementById('btnSync');
            btn.innerText = 'Syncing... Check Terminal';
            btn.disabled = true;
            link.style.pointerEvents = 'none'; 
        }

        function getStagePercentage(day, stageMinutes) {
            const timeInBedMinutes = day.duration_minutes || 0;
            if (timeInBedMinutes === 0) return 0;
            return ((stageMinutes || 0) / timeInBedMinutes) * 100;
        }

        async function fetchData(days) {
            const trendContainer = document.getElementById('trendGraph').parentNode;
            const barContainer = document.getElementById('sleepGraph').parentNode;
            
            trendContainer.innerHTML = '<p>Loading...</p>';
            
            const response = await fetch('/api/sleepdata?days=' + days);
            const rawData = await response.json();
            
            trendContainer.innerHTML = `<canvas id='trendGraph'></canvas>`;
            const barWrapper = document.getElementById('sleepGraph') ? document.getElementById('sleepGraph').parentNode : document.querySelector('.chart-wrapper-bar .chart-container');
            barWrapper.innerHTML = `<canvas id='sleepGraph'></canvas>`;
            
            const pieWrapper = document.getElementById('sleepPieChart') ? document.getElementById('sleepPieChart').parentNode : document.querySelector('.chart-wrapper-pie .chart-container');
            pieWrapper.innerHTML = `<canvas id='sleepPieChart'></canvas>`;

            const durWrapper = document.getElementById('durationGraph') ? document.getElementById('durationGraph').parentNode : document.querySelectorAll('.chart-container')[3];
            durWrapper.innerHTML = `<canvas id='durationGraph'></canvas>`;
            
            if (rawData.length === 0) return;

            const commonLabels = rawData.map(day => day.date ? day.date.substring(5) : 'N/A');

            // --- TREND GRAPH ---
            const ctxTrend = document.getElementById('trendGraph').getContext('2d');
            let gradient = ctxTrend.createLinearGradient(0, 0, 0, 400);
            gradient.addColorStop(0, 'rgba(75, 192, 192, 0.5)'); 
            gradient.addColorStop(1, 'rgba(75, 192, 192, 0.0)'); 

            const trendDataPoints = rawData.map(d => {
                const sleepMins = (d.deep_minutes || 0) + (d.light_minutes || 0) + (d.rem_minutes || 0);
                return sleepMins === 0 ? null : (sleepMins / 60).toFixed(2);
            });

            if (trendChart) { trendChart.destroy(); }

            trendChart = new Chart(ctxTrend, {
                type: 'line',
                data: {
                    labels: commonLabels,
                    datasets: [{
                        label: 'Sleep Duration Trend (Hours)',
                        data: trendDataPoints,
                        borderColor: '#66fcf1', 
                        backgroundColor: gradient, 
                        borderWidth: 2,
                        pointBackgroundColor: '#fff',
                        pointBorderColor: '#66fcf1',
                        pointRadius: 4,
                        fill: true, 
                        tension: 0.4, 
                        spanGaps: false 
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: { display: true, text: 'Sleep Duration Trends', color: '#c5c6c7', font: { size: 16 } },
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const rawVal = parseFloat(context.raw);
                                    const hours = Math.floor(rawVal);
                                    const minutes = Math.round((rawVal - hours) * 60);
                                    return ` ${hours}h ${minutes}m`;
                                }
                            }
                        }
                    },
                    scales: {
                        x: { grid: { color: '#1f2833' }, ticks: { color: '#c5c6c7' } },
                        y: { 
                            grid: { color: '#2b3640' }, 
                            ticks: { color: '#c5c6c7' },
                            title: { display: true, text: 'Hours', color: '#45a29e' }
                        }
                    }
                }
            });

            // --- BAR CHART ---
            const ctxBar = document.getElementById('sleepGraph').getContext('2d');
            const barData = {
                labels: commonLabels,
                datasets: [
                    { label: 'Awake', data: rawData.map(d => getStagePercentage(d, d.wake_minutes)), backgroundColor: 'rgba(255, 99, 132, 0.8)' },
                    { label: 'Light', data: rawData.map(d => getStagePercentage(d, d.light_minutes)), backgroundColor: 'rgba(54, 162, 235, 0.8)' },
                    { label: 'Deep', data: rawData.map(d => getStagePercentage(d, d.deep_minutes)), backgroundColor: 'rgba(75, 192, 192, 0.8)' },
                    { label: 'REM', data: rawData.map(d => getStagePercentage(d, d.rem_minutes)), backgroundColor: 'rgba(153, 102, 255, 0.8)' }
                ].reverse()
            };
            sleepChart = new Chart(ctxBar, { 
                type: 'bar', 
                data: barData, 
                options: { 
                    responsive: true, 
                    maintainAspectRatio: false, 
                    plugins: { title: { display: true, text: 'Daily Sleep Stages %', color: '#fff' } },
                    scales: { x: { stacked: true, grid: { display: false } }, y: { stacked: true, max: 100, grid: { color: '#2b3640' } } } 
                } 
            });

            // --- PIE CHART ---
            let totalDeep = 0, totalLight = 0, totalRem = 0, totalWake = 0;
            rawData.forEach(day => {
                totalDeep += day.deep_minutes || 0;
                totalLight += day.light_minutes || 0;
                totalRem += day.rem_minutes || 0;
                totalWake += day.wake_minutes || 0;
            });

            const grandTotal = totalDeep + totalLight + totalRem + totalWake;
            const deepPct = grandTotal > 0 ? ((totalDeep / grandTotal) * 100).toFixed(1) : 0;
            const lightPct = grandTotal > 0 ? ((totalLight / grandTotal) * 100).toFixed(1) : 0;
            const remPct = grandTotal > 0 ? ((totalRem / grandTotal) * 100).toFixed(1) : 0;
            const wakePct = grandTotal > 0 ? ((totalWake / grandTotal) * 100).toFixed(1) : 0;

            const ctxPie = document.getElementById('sleepPieChart').getContext('2d');
            pieChart = new Chart(ctxPie, {
                type: 'pie',
                data: {
                    labels: ['Awake', 'Light', 'Deep', 'REM'],
                    datasets: [{
                        data: [wakePct, lightPct, deepPct, remPct],
                        backgroundColor: ['rgba(255, 99, 132, 0.8)', 'rgba(54, 162, 235, 0.8)', 'rgba(75, 192, 192, 0.8)', 'rgba(153, 102, 255, 0.8)'],
                        borderColor: '#1f2833',
                        borderWidth: 2
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: { display: true, text: 'Total Avg Composition', color: '#c5c6c7' },
                        legend: { position: 'bottom', labels: { color: '#c5c6c7' } }
                    }
                }
            });

            // --- DURATION BAR ---
            const ctxDur = document.getElementById('durationGraph').getContext('2d');
            const durationInHours = rawData.map(d => {
                const actualSleepMins = (d.deep_minutes || 0) + (d.light_minutes || 0) + (d.rem_minutes || 0);
                return (actualSleepMins / 60).toFixed(2);
            });

            durationChart = new Chart(ctxDur, {
                type: 'bar',
                data: {
                    labels: commonLabels,
                    datasets: [{
                        label: 'Duration (Hours)',
                        data: durationInHours,
                        backgroundColor: 'rgba(255, 206, 86, 0.7)',
                        borderColor: 'rgba(255, 206, 86, 1)',
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true, 
                    maintainAspectRatio: false,
                    plugins: {
                        title: { display: true, text: 'Daily Duration (Bar View)', color: '#c5c6c7' },
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const rawVal = parseFloat(context.raw);
                                    const hours = Math.floor(rawVal);
                                    const minutes = Math.round((rawVal - hours) * 60);
                                    return ` ${hours}h ${minutes}m`;
                                }
                            }
                        }
                    },
                    scales: {
                        x: { grid: { display: false }, ticks: { color: '#c5c6c7' } },
                        y: { grid: { color: '#2b3640' }, ticks: { color: '#c5c6c7' } }
                    }
                }
            });
        }
    </script>
</body>
</html>";
        }
    }
}