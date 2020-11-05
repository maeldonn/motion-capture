function drawChart(elementId, csvName) {
	
	$.get('csv/'+csvName, function(csvString) {
	
		//transform the CSV string into a 2-dimensional array
		var arrayData = $.csv.toArrays(csvString.trim(), {onParseValue: $.csv.hooks.castToScalar});
		
		var arrayDataSimplified = [];
		
		for (let i = 0; i < arrayData.length; i++) {
			if(i%10==0)
			{
				arrayDataSimplified.push(arrayData[i]);
			}
		}
		
		//this new DataTable object holds all the data
		var data = new google.visualization.arrayToDataTable(arrayDataSimplified);

		var options = {
			chart: {
			  title: 'Score of the different movements for '+elementId,
			  subtitle: 'from 0 to 1'
			},
			width: 700,
			height: 500,
			hAxis: {format:'# s'},
			axes: {
			  x: {
				0: {side: 'bottom'}
			  }
			},
			enableInteractivity: true,
			async: true
		};

		var chart = new google.charts.Line(document.getElementById(elementId));

		chart.draw(data, google.charts.Line.convertOptions(options));
	});
}