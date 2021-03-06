var App;
$(function () {
    App = {
        init : function() {
            Quagga.init(this.state, function(err) {
                if (err) {
                    console.log(err);
                    $("#errLog").show();
                    return;
                }
                App.attachListeners();
                Quagga.start();
            });
        },
        attachListeners: function() {
            var self = this;

            $(".bar-close").on("click", function (e) {
                e.preventDefault();
                Quagga.stop();
                //$("#interactive").empty();
            });
            //$(".controls").on("click", "button.start", function (e) {
            //    e.preventDefault();
            //    App.init();
            //});
            //$(".controls .reader-config-group").on("change", "input, select", function(e) {
            //    e.preventDefault();
            //    var $target = $(e.target),
            //        value = $target.attr("type") === "checkbox" ? $target.prop("checked") : $target.val(),
            //        name = $target.attr("name"),
            //        state = self._convertNameToState(name);

            //    console.log("Value of "+ state + " changed to " + value);
            //    self.setState(state, value);
            //});
        },
        //_accessByPath: function(obj, path, val) {
        //    var parts = path.split('.'),
        //        depth = parts.length,
        //        setter = (typeof val !== "undefined") ? true : false;

        //    return parts.reduce(function(o, key, i) {
        //        if (setter && (i + 1) === depth) {
        //            o[key] = val;
        //        }
        //        return key in o ? o[key] : {};
        //    }, obj);
        //},
        //_convertNameToState: function(name) {
        //    return name.replace("_", ".").split("-").reduce(function(result, value) {
        //        return result + value.charAt(0).toUpperCase() + value.substring(1);
        //    });
        //},
        //detachListeners: function() {
        //    $(".controls").off("click", "button.stop");
        //    $(".controls .reader-config-group").off("change", "input, select");
        //},
        //setState: function(path, value) {
        //    var self = this;

        //    if (typeof self._accessByPath(self.inputMapper, path) === "function") {
        //        value = self._accessByPath(self.inputMapper, path)(value);
        //    }

        //    self._accessByPath(self.state, path, value);

        //    console.log(JSON.stringify(self.state));
        //    App.detachListeners();
        //    Quagga.stop();
        //    App.init();
        //},
        //inputMapper: {
        //    inputStream: {
        //        constraints: function(value){
        //            var values = value.split('x');
        //            return {
        //                width: parseInt(values[0]),
        //                height: parseInt(values[1])
        //            }
        //        }
        //    },
        //    numOfWorkers: function(value) {
        //        return parseInt(value);
        //    },
        //    decoder: {
        //        readers: function(value) {
        //            if (value === 'ean_extended') {
        //                return [{
        //                    format: "ean_reader",
        //                    config: {
        //                        supplements: [
        //                            'ean_5_reader', 'ean_2_reader'
        //                        ]
        //                    }
        //                }];
        //            }
        //            return [{
        //                format: value + "_reader",
        //                config: {}
        //            }];
        //        }
        //    }
        //},
        state: {
            inputStream: {
                type : "LiveStream",
                constraints: {
                    width: 640,
                    height: 480,
                    facingMode: "environment" // or user
                }
            },
            locator: {
                patchSize: "medium",
                halfSample: true
            },
            numOfWorkers: 1,
            decoder: {
                readers : [{
                    format: "code_128_reader",
                    config: {}
                }]
            },
            locate: true
        },
        lastResult : null
    };

    //App.init();

    Quagga.onProcessed(function(result) {
        var drawingCtx = Quagga.canvas.ctx.overlay,
            drawingCanvas = Quagga.canvas.dom.overlay;

        if (result) {
            if (result.boxes) {
                drawingCtx.clearRect(0, 0, parseInt(drawingCanvas.getAttribute("width")), parseInt(drawingCanvas.getAttribute("height")));
                result.boxes.filter(function (box) {
                    return box !== result.box;
                }).forEach(function (box) {
                    Quagga.ImageDebug.drawPath(box, {x: 0, y: 1}, drawingCtx, {color: "green", lineWidth: 2});
                });
            }

            if (result.box) {
                Quagga.ImageDebug.drawPath(result.box, {x: 0, y: 1}, drawingCtx, {color: "#00F", lineWidth: 2});
            }

            if (result.codeResult && result.codeResult.code) {
                Quagga.ImageDebug.drawPath(result.line, {x: 'x', y: 'y'}, drawingCtx, {color: 'red', lineWidth: 3});
            }
        }
    });

    Quagga.onDetected(function(result) {
        var code = result.codeResult.code;
        if (App.lastResult !== code) {
            App.lastResult = code;
            var regx = /^[A-Za-z]+[0-9]+$/;
            if (regx.test(code)) {                
                uploadAjax(code);//页面中的方法,正则验证通过后和服务器通信
            }            

        //    var $node = null, canvas = Quagga.canvas.dom.image;

        //    $node = $('<li><div class="thumbnail"><div class="imgWrapper"><img /></div><div class="caption"><h4 class="code"></h4></div></div></li>');
        //    $node.find("img").attr("src", canvas.toDataURL());
        //    $node.find("h4.code").html(code);
        //    $("#result_strip ul.thumbnails").prepend($node);
        }
    });
});
