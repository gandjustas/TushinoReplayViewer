var Sizes = { "altis": [30721, 30721], "bootcamp_acr": [3843, 3847], "bornholm": [22529, 22529], "chernarus": [15361, 15361], "chernarus_summer": [15361, 15361], "desert_e": [2049, 2049], "intro": [5121, 5121], "kunduz": [5124, 5129], "mountains_acr": [6410, 6403], "porto": [5121, 5121], "provinggrounds_pmc": [2054, 2049], "shapur_baf": [2049, 2049], "stratis": [8193, 8193], "takistan": [12801, 12803], "woodland_acr": [7683, 7682], "zargabad": [8193, 8196], "smd_sahrani_a3": [20481, 20481], "utes": [5121, 5121], "mbg_celle2": [12292, 12298], "fata": [5119, 5119], "napf": [20481, 20481] };
var Peaks = {};
var MapName
var MapSize
var replay;
var map;

var MP = {
    'status': false,
    'server': ''
};

var Sides = [
    "WEST",
    "EAST",
    "GUER",
    "LOGIC"
];
//var socket = io.connect('http://37.143.14.33:8080');


//socket.on('connect', function () {
//	$('.mps').show();
//	socket.emit('join');

//	$(document).on('click', '#multi', function(){
//		var file = window.location.search.substr(6);
//		socket.emit('srvcreate', file);
//		MP['status'] = true;
//	});

//	socket.on('srvhash', function (hash) {
//		window.location.hash = "";
//		$('#srvhash').val(window.document.location.href+'server='+hash);
//	});

//	socket.on('tock', function (sec) {
//		replay.sec = sec;
//		if(replay.pause){
//			replay.start();
//		}
//		replay.tick(replay.sec);
//	});

//	var hash = window.location.hash.replace(/#/g,"").split('=');
//	if((hash[0] == 'server') && (hash[1].length)) {
//		$('.mps').hide();
//		MP['server'] = hash[1];
//		socket.emit('join', MP['server']);
//		MP['status'] = true;
//	}
//});



var Replay = function (replayUrl, array) {
    if (array.length < 2) {
        this.toLog($('#messages'), this.timeSecToStr(0) + ' Реплей пуст.', 0);
        alert('Реплей пуст.');
        return;
    }

    this.replayUrl = replayUrl;
    this.log = array;
    this.speed = 1;
    this.pause = false;
    this.hideDead = false;
    this.deadLine = [];
    this.initInfo();
    this.initTriggers();

    this.sec = 1;
    this.Players = [];
    this.processFrame(this.log[1]);

    this.killBoard = $('#kills');

    this.initJS();
    this.start();

};

Replay.prototype.getStateHash = function () {
    var x = {
        t: this.sec
    };
    if (this.pause) x.pause = 1;

    var s = [];
    for (var k in x) {
        s.push(k + '=' + x[k]);
    }

    return s.join('&');
}

Replay.prototype.setWindowUrl = function () {
    var q = "?p=" + this.replayUrl;
    if (q != document.location.search) {
        window.history.replaceState(null, "Tushino Online Replay Viewer", q);
    }

    document.location.hash = this.getStateHash();

}

Replay.prototype.rewind = function (s) {
    var k = this.sec + 1;
    if (s < this.sec) {
        $('#kills').text('');
        $('#names').text('');
        $('#messages').text('');
        this.Players.forEach(function (p, index, arr) {
            if (p.marker) map.removeLayer(p.marker);
        });
        this.Players = [];
        k = 1;
    }
    for (var i = k; i < s; i++) {
        this.processFrame(this.log[i]);
    }
    this.updateMarkers();
    this.sec = s;

    $('#time-replay').text(this.timeSecToStr(this.log[s][0]));
    $('#slider').slider("value", this.sec);
    this.setWindowUrl();
}

Replay.prototype.initInfo = function () {
    this.ReplayInfo = this.log[0];
    $('.name-replay').text(this.ReplayInfo[1]);
    $('.date-replay').text(this.ReplayInfo[2]);
};

Replay.prototype.initPeaks = function () {
    var p = Peaks[MapName];
    p.forEach(function (item, i, array) {
        var icon = L.divIcon({
            html: '<span class="peaks">' + Math.round(item[2]) + '</span>'
        });
        L.marker(map.unproject([item[0], MapSize[1] - item[1]], map.getMaxZoom()), {
            icon: icon
        }).addTo(map);
    });
};

Replay.prototype.initObjNames = function () {
    if (typeof obj == 'undefined')
        return;
    obj.forEach(function (item, i, array) {
        var icon = L.divIcon({
            html: '<span class="geo ' + item['type'] + '">' + item['name'] + '</span>'
        });
        L.marker(map.unproject([item['position'][0], MapSize[1] - item['position'][1]], map.getMaxZoom()), {
            icon: icon
        }).addTo(map);
    });
};

Replay.prototype.initJS = function () {
    var parent = this;
    $(document).on('click', '#play-pause', function () {
        if (parent.pause === false) {
            parent.stop();
        } else {
            parent.start();
        }
        event.preventDefault();
    });
    $(document).on('click', '.speed-btn button', function () {
        $('.speed-btn button').removeClass('active');
        $(this).addClass('active');
        parent.speed = $(this).attr('rel');
        event.preventDefault();
    });
    $('#slider').slider({
        min: 1,
        max: parent.log.length,
        slide: function (event, ui) {
            parent.rewind(ui.value);
            if (parent.isFinished) {
                parent.start();
                parent.isFinished = false;
            }

        }
    });
    $(document).tooltip();
};

Replay.prototype.tick = function () {
    if (this.pause) return;
    var frame = this.log[this.sec];
    if (!frame) {
        this.toLog($('#messages'), this.timeSecToStr(this.log[this.sec - 1][0]) + ' : КОНЕЦ', 0);
        this.isFinished = true;
        return;
    }
    this.deadLine.forEach(function (line, index, arr) {
        map.removeLayer(line);
    });
    if (frame[0] == -1) {
        this.toLog($('#messages'), this.timeSecToStr(this.log[this.sec - 1][0]) + ' : ' + this.log[this.sec][1], 0);
    } else {
        var parent = this;
        $('#time-replay').text(this.timeSecToStr(frame[0]));
        $('#slider').slider("value", parent.sec);
        this.processFrame(frame);
        this.updateMarkers();
        this.updatePlayersCount();
    }

    if (this.hideDead) {
        $('.unit-dead').hide();
    } else {
        $('.unit-dead').show();
    }

    this.setWindowUrl();

    if (MP['server'].length == 0) {
        var parent = this;
        setTimeout(function () {
            parent.tick(parent.sec++);
            if ((MP['status'] == true) && (MP['server'].length == 0)) {
                //socket.emit('tick', parent.sec);
            }
        }, (1000 / this.speed));
    }
}

Replay.prototype.start = function () {
    this.pause = false;
    this.tick();
    $('#play-pause-icon').removeClass('fa-play');
    $('#play-pause-icon').addClass('fa-pause');
    $('#play-pause span').text('Пауза');
}

Replay.prototype.stop = function () {
    this.pause = true;
    $('#play-pause-icon').removeClass('fa-pause');
    $('#play-pause-icon').addClass('fa-play');
    $('#play-pause span').text('Старт');
    this.setWindowUrl();
}

Replay.prototype.toLog = function (div, string, pos) {
    if (pos) {
        $(div)
            .prepend($("<p></p>")
                .html(string));
    } else {
        $(div)
            .append($("<p></p>")
                .html(string));
    }
}

Replay.prototype.drawBox = function (xy, s, angle, color) {
    if (typeof (Number.prototype.toRad) === "undefined") {
        Number.prototype.toRad = function () {
            return this * Math.PI / 180;
        }
    }
    var x1 = xy[0] - s[0];
    var y1 = MapSize[1] - (xy[1] - s[1]);
    var x2 = xy[0] + s[0];
    var y2 = MapSize[1] - (xy[1] - s[1]);
    var x3 = xy[0] + s[0];
    var y3 = MapSize[1] - (xy[1] + s[1]);
    var x4 = xy[0] - s[0];
    var y4 = MapSize[1] - (xy[1] + s[1]);
    if (angle) {
        var cx = x4 + ((x2 - x4) / 2);
        var cy = y4 + ((y2 - y4) / 2);
        var sin = Math.sin(angle.toRad());
        var cos = Math.cos(angle.toRad());
        var tmp_x1 = x1 - cx;
        var tmp_y1 = cy - y1;
        var tmp_x2 = x2 - cx;
        var tmp_y2 = cy - y2;
        var tmp_x3 = x3 - cx;
        var tmp_y3 = cy - y3;
        var tmp_x4 = x4 - cx;
        var tmp_y4 = cy - y4;

        x1 = tmp_x1 * cos + tmp_y1 * sin + cx;
        y1 = cy + tmp_x1 * sin - tmp_y1 * cos;
        x2 = tmp_x2 * cos + tmp_y2 * sin + cx;
        y2 = cy + tmp_x2 * sin - tmp_y2 * cos;
        x3 = tmp_x3 * cos + tmp_y3 * sin + cx;
        y3 = cy + tmp_x3 * sin - tmp_y3 * cos;
        x4 = tmp_x4 * cos + tmp_y4 * sin + cx;
        y4 = cy + tmp_x4 * sin - tmp_y4 * cos;
    }
    return L.polygon([
        map.unproject([x1, y1], map.getMaxZoom()),
        map.unproject([x2, y2], map.getMaxZoom()),
        map.unproject([x3, y3], map.getMaxZoom()),
        map.unproject([x4, y4], map.getMaxZoom()),
    ], { color: color, stroke: false, fillOpacity: 0.7, className: 'map-marker' }).addTo(map);
}

Replay.prototype.drawDot = function (xy, color) {
    var icon = L.divIcon({
        iconSize: 10,
        iconAnchor: [5, 5],
        html: '<div style="width:10px;height:10px;border-radius:50%;background:' + color + '"></div>'
    });
    return L.marker(map.unproject([xy[0], MapSize[1] - xy[1]], map.getMaxZoom()), {
        icon: icon
    }).addTo(map);
}

/*
Replay.prototype.drawEllipse = function(xy, s, angle, color){
	console.log(s);
	return L.ellipse(map.unproject([xy[0], MapSize[1]-xy[1]], map.getMaxZoom()), [750*625, 750*625], 0).addTo(map);
}*/

Replay.prototype.timeSecToStr = function (data) {
    var minuts = Math.floor(data / 60);
    var hours = Math.floor(minuts / 60);
    var seconds = data - (minuts * 60);
    minuts = minuts - (hours * 60);
    seconds = (seconds < 10) ? "0" + seconds.toString() : seconds.toString();
    minuts = (minuts < 10) ? "0" + minuts.toString() : minuts.toString();
    hours = (hours < 10) ? "0" + hours.toString() : hours.toString();

    return hours + ":" + minuts + ":" + seconds;
}


Replay.prototype.processPlayers = function (frame) {
    var that = this;
    var dict = {};
    frame.forEach(function (p, index, arr) {
        if (index < 2) return;
        var id = p[0];
        dict[id] = true;

        var player = that.Players[id];
        if (!player) return;

        player.vehicleOrDriver = p[6];

        if (p[5] == 1) {
            player.dead = true;
        } else {
            player.dead = false;
        }
        player.azimuth = p[4];
        player.x = p[1];
        player.y = p[2];
    });

    that.Players.forEach(function (p, index, arr) {
        if (!dict[index]) {
            if (p.marker) map.removeLayer(p.marker);
            delete arr[index];
        }
    });
}

Replay.prototype.updateMarkers = function () {
    var that = this;
    this.Players.forEach(function (player, index, arr) {
        var classunit = (player.dead) ? 'dead' : player.side;
        var globclass = (player.dead) ? 'unit-dead' : 'unit';
        var name = classNames[player.name] || player.name;

        if (player.vehicleOrDriver) {
            if (player.icon == "Man") {
                globclass += ' in-veh';
            } else {
                var driver = that.Players[player.vehicleOrDriver];
                if (driver) {
                    name += ' (' + driver.name + ')';
                    classunit = driver.side;
                }
            }
        }
        var icon = L.divIcon({
            iconSize: 15,
            iconAnchor: [7, 7],
            html: '<span class="' + globclass + '">' +
            '<span class="unit-player unit-player-' + classunit + '" style="transform: rotate(' + player.azimuth + 'deg);"></span>' +
            '<span class="unit-name unit-name-' + classunit + '">' + name + '</span>' +
            '</span>'
        });
        /*var html = player[2]+'<br>';
            if(player[4] == "Man"){
                html += '<img width="200" src="http://tsgdb.bystolen.ru/core/images/units/'+player[2]+'.jpg"><br>';
            }
        html += 'Здоровье: <div class="progress progress-danger"><div class="bar" style="width:'+Math.round((1-p[5])*100)+'%">'+Math.round((1-p[5])*100)+'%</div></div>';*/
        if (typeof player.marker == 'object') {
            player.marker.setLatLng(map.unproject([player.x, MapSize[1] - player.y], map.getMaxZoom())).setIcon(icon)/*.bindPopup(html)*/.update();
        } else {
            player.marker = L.marker(map.unproject([player.x, MapSize[1] - player.y], map.getMaxZoom()), {
                icon: icon
            }).addTo(map);
        }
    });
}
Replay.prototype.processFrame = function (frame) {
    this.processEvents(frame[0], frame[1]);
    this.processPlayers(frame)
}

Replay.prototype.processEvents = function (time, events) {
    //var plrs = new Array([]);
    var parent = this;
    events.forEach(function (message, index, arr) {
        var player = [];
        if (message[0] == 1) {
            var id = message[1];
            parent.Players[id] = {
                id: id,
                type: 0,
                name: message[2],
                className: message[3],
                side: Sides[message[4]],
                icon: message[5],
                squad: message[6],
                slot: message[7]
            };
        } else if (message[0] == 2) {
            var id = message[1];
            parent.Players[id] = {
                id: id,
                type: 1,
                name: message[2],
                className: message[2],
                side: 'CIV',
                icon: message[3],
            }
        } else if (message[0] == 3) {
            parent.Players[message[1]].name = message[3];
            if (time > 0) {
                parent.toLog($('#names'), parent.timeSecToStr(time) + ' : ' + message[2] + ' ==> ' + message[3], 0);
            }
        } else if (message[0] == 4) {
            if (message[2] == 0 || message[2] == message[3]) {
                parent.toLog(parent.killBoard, parent.timeSecToStr(message[1]) + ' : ' + parent.Players[message[3]].name + ' умер', 1);
            } else {
                var kill = [];
                parent.Players[message[3]].dead = true;
                if (parent.Players[message[2]].side == parent.Players[message[3]].side) {
                    parent.toLog(parent.killBoard, parent.timeSecToStr(message[1]) + ' : ' + parent.Players[message[2]].name + ' убил ' + parent.Players[message[3]].name + ' с помощью ' + message[4] + ' с расстояния ' + message[6] + ' <b style="color:red">ТИМКИЛЛ</b>', 1);
                } else {
                    parent.toLog(parent.killBoard, parent.timeSecToStr(message[1]) + ' : ' + parent.Players[message[2]].name + ' убил ' + parent.Players[message[3]].name + ' с помощью ' + message[4] + ' с расстояния ' + message[6], 1);
                }
                parent.deadLine.push(parent.drawShot(parent.Players[message[2]], parent.Players[message[3]]));
            }
        }

    });
    //console.log(this.Players);
    //this.Players.push(plrs);
}

Replay.prototype.drawShot = function (killer, victim) {
    if (typeof victim == 'undefined') {
        victim = killer;
    }
    var colr = '';
    switch (killer.side) {
        case "EAST":
            colr = 'red';
            break;
        case "WEST":
            colr = 'blue';
            break;
        case "GUER":
            colr = 'green';
            break;
        case "LOGIC", "CIVI", "CIV":
            colr = '#989D46';
            break;
        default:
            colr = 'red';
    }
    return L.polyline([map.unproject([killer.x, MapSize[1] - killer.y], map.getMaxZoom()), map.unproject([victim.x, MapSize[1] - victim.y], map.getMaxZoom())], { color: colr, weight: 3, opacity: 0.7 }).addTo(map);
}


Replay.prototype.initTriggers = function () {
    var patern = this;
    this.ReplayInfo[5].forEach(function (marker, index, arr) {
        console.log(marker);
        if (marker[1] == "RECTANGLE") {
            patern.drawBox(marker[5], marker[6], marker[7], marker[4].replace('Color', ''));
        } else if (marker[1] == "ICON" && marker[2] == "mil_dot") {
            patern.drawDot(marker[5], marker[4].replace('Color', ''));
        }
        /*else if(marker[1] == "ELLIPSE") {
			console.log(marker);
			//L.ellipse(map.unproject([0, 0], map.getMaxZoom()), [500, 100], 90).addTo(map);
		}*/

    });
};

Replay.prototype.updatePlayersCount = function () {
    var sides = {};
    this.Players.forEach(function (player, index, arr) {
        if (player.side == 'EAST' || player.side == 'WEST' || player.side == 'GUER') {
            if (!player.dead && player.icon == 'Man' && player.name.length) {
                if (!sides[player.side]) {
                    sides[player.side] = 1;
                } else {
                    sides[player.side]++;
                }
            }
        }
    });
    var html = '';
    for (var side in sides) {
        switch (side) {
            case "EAST":
                colr = 'red';
                name = 'Красных:';
                break;
            case "WEST":
                colr = 'blue';
                name = 'Синих:';
                break;
            case "GUER":
                colr = 'green';
                name = 'Зелёных:';
                break;
        }
        html += '<span style="color:' + colr + '">' + name + '</span> ' + sides[side] + '<br>';
    }
    $('.players_cnt').html(html);
}

function loadReplay(qs, hash) {
    var params = qs.substr(1).split("&").reduce(function (x, p) {
        var pair = p.split('=');
        x[pair[0]] = decodeURIComponent(pair[1]);
        return x;
    }, {});

    if (hash) {
        params = hash.substr(1).split("&").reduce(function (x, p) {
            var pair = p.split('=');
            x[pair[0]] = decodeURIComponent(pair[1]);
            return x;
        }, params);
    }

    $.ajax("/api/replay" + qs, { dataType: 'json' })
        .fail(function (jqXHR, textStatus, errorThrown) {
            appInsights.trackException(errorThrown);
            alert(errorThrown);
        })
        .done(function (data, status, xhr) {
            MapName = data[0][0].toLowerCase();
            MapSize = Sizes[MapName];



            map = L.map('map', {
                worldCopyJump: false,
                zoomAnimation: false,
                keyboard: false,
                zoomControl: true,
                crs: L.CRS.Simple
            });



            /*
            map.on('zoomend', function(e) {
                console.log(map.getZoom());
            });
    
            map.on('click', function(e) {
                console.log(map.getMaxZoom());
            });
            */
            var urlTemplate = 'https://replayviewer.blob.core.windows.net/maps/' + MapName + '/{z}/{x}_{y}.png';
            if (params['new']) {
                urlTemplate = 'https://replayviewer.blob.core.windows.net/maps-new/' + MapName + '/{z}/{y}_{x}.png';
            }

            L.tileLayer(urlTemplate, {
                minZoom: 4,
                maxZoom: params['new'] ? 7 : 7,
                continuousWorld: true,
                noWrap: true,
                errorTileUrl: '/images/blank.png',
                attribution: "Online replay viewer by <a href='http://серьёзныеигры.рф/index.php?subaction=userinfo&user=Stolen' target='_blank'>Stolen</a> and <a href='http://серьёзныеигры.рф/index.php?subaction=userinfo&user=hitman' target='_blank'>hitman</a> "
            }).addTo(map);

            map.setView(map.unproject([MapSize[0] / 2, MapSize[1] / 2], map.getMaxZoom()), map.getMinZoom());

            map.on('mousemove', function (e) {
                var point = map.project(e.latlng, map.getMaxZoom());
                $('.coordx').text(parseInt(point['x']/*/100*/));
                $('.coordy').text(parseInt((MapSize[1] - point['y'])/*/100*/));
            });

            replay = new Replay(params.p, data);
            if (params.t) { replay.rewind(params.t) };
            if (params.pause == 1) { replay.stop(); };
        })
}

$(document).ready(function () {
    var qs = document.location.search;
    if (qs) loadReplay(qs, document.location.hash);


    $(document).on('change', 'input[name=showdead]', function () {
        if ($(this).is(':checked')) {
            replay.hideDead = false;
        } else {
            replay.hideDead = true;
        }
    });

    $(document).on('change', 'input[name=geo]', function () {
        if ($(this).is(':checked')) {
            $('.geo').show();
        } else {
            $('.geo').hide();
        }
    });

    $(document).on('change', 'input[name=peaks]', function () {
        if ($(this).is(':checked')) {
            $('.peaks').show();
        } else {
            $('.peaks').hide();
        }
    });

    $(document).on('change', 'input[name=markers]', function () {
        if ($(this).is(':checked')) {
            $('.map-marker').show();
        } else {
            $('.map-marker').hide();
        }
    });
});