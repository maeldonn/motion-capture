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

function changeYtVideo(elementId, animationAndName){
	
	var newSrc = "";
	
	switch(animationAndName){
		case "ChairMael":
			newSrc+="https://www.youtube.com/embed/Kz9Aw75T9I4";
			break;
		case "ChairOscar":
			newSrc+="https://www.youtube.com/embed/akSPM5uRfX0";
			break;
		case "DoorMael":
			newSrc+="https://www.youtube.com/embed/hiwTt7Tvcq4";
			break;
		case "DoorOscar":
			newSrc+="https://www.youtube.com/embed/unygPkxP10Y";
			break;
		case "SalutingMael":
			newSrc+="https://www.youtube.com/embed/ZXZITcUFRSc";
			break;
		case "SalutingOscar":
			newSrc+="https://www.youtube.com/embed/YRBNX5z3W8s";
			break;
		case "SequenceMael":
			newSrc+="https://www.youtube.com/embed/V41M1reBkdo";
			break;
		case "SequenceOscar":
			newSrc+="https://www.youtube.com/embed/wCnqThEJaRY";
			break;
		case "WalkingMael":
			newSrc+="https://www.youtube.com/embed/XGVayIFEVWg";
			break;
		case "WalkingOscar":
			newSrc+="https://www.youtube.com/embed/i6neqg-fi_E";
			break;
		default:
			console.log("Error : no Youtube video corresponds to your request");
			break;
	}
	
	
	document.getElementById(elementId).src = newSrc;
}