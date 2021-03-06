var hiddenFromLeft = [];
var hiddenFromRight = [];
var visibleAreaWidth = 0;
function updateBreadCrumbsLabels(newBreadCrumbs) {
    if(newBreadCrumbs) {
        if(breadCrumbs == null) {
            breadCrumbs = newBreadCrumbs;
            for(i = 0; i < breadCrumbs.length; i++) {
                addBreadCrumb(breadCrumbs[i].vcElement);
            }
            moveToRightEdge();
            return;
        }
        for(i = 0; i < breadCrumbs.length; i++) {
            if(newBreadCrumbs[i] == null) {
                removeBreadCrumb();
            } else if(newBreadCrumbs[i].vcElement.id != breadCrumbs[i].vcElement.id) {
                for(j = i; j < breadCrumbs.length; j++) {
                    removeBreadCrumb();
                }
                for(j = i; j < newBreadCrumbs.length; j++) {
                    addBreadCrumb(newBreadCrumbs[j].vcElement);
                }
                breadCrumbs = newBreadCrumbs;
                return;
            }
        }
        moveToRightEdge();
        for(i = breadCrumbs.length; i < newBreadCrumbs.length; i++) {
            addBreadCrumb(newBreadCrumbs[i].vcElement);
        }
        moveToRightEdge();
        breadCrumbs = newBreadCrumbs;
    }
}
function updateHiddenBreadCrumbs() {
    hiddenFromLeft = [];
    hiddenFromRight = [];
    var tableOffset = $(".breadCrumbTable").position().left;
    $(".breadCrumbTable tr td").each(function (index) {
        if($(this).attr("moving") != "left" && $(this).attr("moving") != "right") {
            var elementOffset = $(this).position().left + tableOffset;
            var elementWidth = $(this).width();
            if(index == 0) {
                if(elementOffset < 0) {
                    hiddenFromLeft.push(index);
                }
            } else if(index == $(".breadCrumbTable tr td").length - 1) {
                if(elementOffset + elementWidth > visibleAreaWidth) {
                    hiddenFromRight.push(index);
                }
            } else {
                if(elementOffset + elementWidth / 3 < 0) {
                    hiddenFromLeft.push(index);
                } else if(elementOffset + elementWidth * 2 / 3 > visibleAreaWidth) {
                    hiddenFromRight.push(index);
                }
            }
        }
    });
    hiddenFromRight.reverse();
    if(hiddenFromLeft.length != 0) {
        $("#bc_navLeft").stop(true, true).fadeIn('fast');
    } else {
        $("#bc_navLeft").stop(true, true).fadeOut('fast');
    }
    if(hiddenFromRight.length != 0) {
        $("#bc_navRight").stop(true, true).fadeIn('fast');
    } else {
        $("#bc_navRight").stop(true, true).fadeOut('fast');
    }
}
function showHiddenBreadCrumb(direction, index) {
    if(index == null) {
        updateHiddenBreadCrumbs();
        switch(direction) {
            case "left":
                if(hiddenFromLeft.length != 0) {
                    index = hiddenFromLeft.pop();
                } else {
                    return;
                }
                break;
            case "right":
                if(hiddenFromRight.length != 0) {
                    index = hiddenFromRight.pop();
                } else {
                    return;
                }
                break;
        }
    }
    $(".breadCrumbTable").stop();
    var element = $("#bc_" + index);
    var tableOffset = $(".breadCrumbTable").position().left;
    var elementOffset = element.position().left + tableOffset;
    var offset = 0;
    switch(direction) {
        case "left":
            offset = -elementOffset;
            break;
        case "right":
            var elementWidth = element.width();
            offset = visibleAreaWidth - elementOffset - elementWidth - 1;
            break;
    }
    if(offset != 0) {
        var str = "+=" + offset + "px";
        element.attr("moving", direction);
        $(".breadCrumbTable").animate({
            "left": str
        }, "slow", function () {
            $(".breadCrumbTable tr td").each(function () {
                $(this).attr("moving", "false");
            });
            updateHiddenBreadCrumbs();
        });
    }
}
function moveToRightEdge(callback) {
    var tableOffset = $(".breadCrumbTable").position().left;
    var tableWidth = $(".breadCrumbTable").width();
    if(tableOffset <= 0) {
        var tableVisible = tableWidth + tableOffset;
        var difference = 0;
        if(tableWidth >= visibleAreaWidth) {
            if(tableVisible > visibleAreaWidth) {
                difference = visibleAreaWidth - tableVisible - 1;
            } else {
                difference = visibleAreaWidth - tableVisible - 1;
            }
        } else {
            difference = -tableOffset;
        }
        $(".breadCrumbTable").stop();
        if(difference != 0) {
            var str = "+=" + difference + "px";
            $(".breadCrumbTable").animate({
                "left": str
            }, "fast", function () {
                $(".breadCrumbTable tr td").each(function () {
                    $(this).attr("moving", "false");
                });
                updateHiddenBreadCrumbs();
                if(callback != null) {
                    callback();
                }
            });
        }
    }
}
function removeBreadCrumb() {
    var length = $(".breadCrumbTable tr td").length;
    if(length > 0) {
        var selector = "#bc_" + (length - 1);
        $(selector).remove();
        if(length > 1) {
            selector = "#bc_" + (length - 2);
            $(selector + " .breadCrumbSeparator").hide();
        }
    }
}
function addBreadCrumb(element) {
    var length = $(".breadCrumbTable tr td").length;
    var parent = $(".breadCrumbTable tr");
    var td = $("<td class='breadCrumbTableCell' id='bc_" + length + "'></td>");
    var div = $("<div class='breadCrumbLink' id='bc_link_" + element.id + "' onclick='clickOverBreadCrumb(\"" + element.id + "\", " + length + ")'></div>");
    var span = $("<span class='breadCrumbSeparator' id='bc_'>&rsaquo;</span>");
    td.append(div);
    td.append(span);
    parent.append(td);
    div.text(element.title);
    switch(element.regime) {
        case "Cosmos":
            $("#bc_link_" + element.id).addClass("breadCrumbLinkCosmosRegime");
            break;
        case "Earth":
            $("#bc_link_" + element.id).addClass("breadCrumbLinkEarthRegime");
            break;
        case "Life":
            $("#bc_link_" + element.id).addClass("breadCrumbLinkLifeRegime");
            break;
        case "Pre-history":
            $("#bc_link_" + element.id).addClass("breadCrumbLinkPreHumanRegime");
            break;
        case "Humanity":
            $("#bc_link_" + element.id).addClass("breadCrumbLinkHumanityRegime");
            break;
    }
    $("#bc_" + length + " .breadCrumbSeparator").hide();
    if(length > 0) {
        $("#bc_" + (length - 1) + " .breadCrumbSeparator").show();
    }
    $("#bc_link_" + element.id).mouseover(function () {
        breadCrumbMouseOver(this);
    });
    $("#bc_link_" + element.id).mouseout(function () {
        breadCrumbMouseOut(this);
    });
}
function breadCrumbNavLeft() {
    var movingLeftBreadCrumbs = 0;
    var number = 0;
    $(".breadCrumbTable tr td").each(function (index) {
        if($(this).attr("moving") == "left") {
            movingLeftBreadCrumbs++;
            if(number == 0) {
                number = index;
            }
        }
    });
    if(movingLeftBreadCrumbs == navigateNextMaxCount) {
        var index = number - longNavigationLength;
        if(index < 0) {
            index = 0;
        }
        showHiddenBreadCrumb("left", index);
    } else if(movingLeftBreadCrumbs < navigateNextMaxCount) {
        showHiddenBreadCrumb("left");
    }
}
function breadCrumbNavRight() {
    var movingRightBreadCrumbs = 0;
    var number = 0;
    $(".breadCrumbTable tr td").each(function (index) {
        if($(this).attr("moving") == "right") {
            movingRightBreadCrumbs++;
            number = index;
        }
    });
    if(movingRightBreadCrumbs == navigateNextMaxCount) {
        var index = number + longNavigationLength;
        if(index >= $(".breadCrumbTable tr td").length) {
            index = $(".breadCrumbTable tr td").length - 1;
        }
        showHiddenBreadCrumb("right", index);
    } else if(movingRightBreadCrumbs < navigateNextMaxCount) {
        showHiddenBreadCrumb("right");
    }
}
function clickOverBreadCrumb(timelineID, breadCrumbLinkID) {
    goToSearchResult(timelineID);
    var selector = "#bc_" + breadCrumbLinkID;
    var tableOffset = $(".breadCrumbTable").position().left;
    var elementOffset = $(selector).position().left + tableOffset;
    var elementWidth = $(selector).width();
    if(elementOffset < 0) {
        showHiddenBreadCrumb("left", breadCrumbLinkID);
    } else if(elementOffset + elementWidth > visibleAreaWidth) {
        showHiddenBreadCrumb("right", breadCrumbLinkID);
    }
}
function breadCrumbMouseOut(element) {
    $(element).removeClass("breadCrumbHover");
}
function breadCrumbMouseOver(element) {
    $(element).addClass("breadCrumbHover");
}
function changeToOff(element) {
    var src = element.getAttribute("src");
    element.setAttribute("src", src.replace("_on", "_off"));
}
function changeToOn(element) {
    var src = element.getAttribute("src");
    element.setAttribute("src", src.replace("_off", "_on"));
}
