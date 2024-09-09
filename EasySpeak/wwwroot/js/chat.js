"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.start().catch(function (err) {
    return console.error(err.toString());
});

connection.on("ReceiveNotification", function (message) {
    Swal.fire({
        icon: 'info',
        title: 'Notification',
        text: message,
    }).then((result) => {
        if (result.isConfirmed) {
            // Reload the page without using cache
            window.location.reload(true);
        }
    });
});

connection.on("ReceiveUpdate", function (user) {
    //document.querySelector(".user-profile span").innerText = user.userName;
    //document.querySelector(".user-profile small").innerText = user.email;
    document.querySelector("#nmid").innerText = user.name;
});

connection.on("NewChatRoom", function (room) {
    Swal.fire({
        icon: 'success',
        title: 'New Chat Room',
        text: 'A new chat room named "' + room.name + '" has been created.',
    }).then(() => {
        addChatRoomToList(room);
    });
});

connection.on("ReceiveUpdateChatRoom", function (updatedRoom) {
    Swal.fire({
        icon: 'info',
        title: 'Chat Room Updated',
        text: 'The chat room "' + updatedRoom.name + '" has been updated.',
    }).then(() => {
        updateChatRoomInList(updatedRoom);
    });
});

connection.on("ReceiveDeleteChatRoom", function (roomId) {
    Swal.fire({
        icon: 'warning',
        title: 'Chat Room Deleted',
        text: 'The chat room with ID ' + roomId + ' has been deleted.',
    }).then(() => {
        removeChatRoomFromList(roomId);
        const currentRoomId = getCurrentRoomId();
        if (currentRoomId === roomId) {
            Swal.fire({
                icon: 'warning',
                title: 'Redirection',
                text: "The chat room you were in has been deleted. You will be redirected.",
            }).then(() => {
                window.location.href = "/Home/Index";
            });
        }
    });
});
connection.on("Logout", function () {
    Swal.fire({
        icon: 'error',
        title: 'Blocked and Logged Out',
        text: 'You have been blocked and will be logged out.',
    }).then(() => {
        fetch('/Identity/Account/Logout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        }).then(response => {
            if (response.ok) {
                window.location.href = '/';
            } else {
                console.error('Logout failed');
            }
        }).catch(error => {
            console.error('Logout error:', error);
        });
    });
});
//connection.on("Logout", function () {
//    alert("You have been blocked and will be logged out.");
//    fetch('/Identity/Account/Logout', {
//        method: 'POST',
//        headers: {
//            'Content-Type': 'application/json',
//            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
//        }
//    }).then(response => {
//        if (response.ok) {
//            window.location.href = '/';
//        } else {
//            console.error('Logout failed');
//        }
//    }).catch(error => {
//        console.error('Logout error:', error);
//    });
//});

function addChatRoomToList(room) {
    const chatRoomList = document.querySelector(".list-group-flush");
    const newRoomElement = document.createElement("a");
    newRoomElement.href = "javascript:void(0);";
    newRoomElement.className = "list-group-item list-group-item-action";
    newRoomElement.ondblclick = function () {
        confirmJoinGroup(room.name);
    };
    newRoomElement.textContent = room.name;
    newRoomElement.dataset.id = room.id;
    chatRoomList.appendChild(newRoomElement);
}

function updateChatRoomInList(room) {
    const chatRoomListItems = document.querySelectorAll(".list-group-item-action");
    chatRoomListItems.forEach(item => {
        if (item.dataset.id === room.id.toString()) {
            item.textContent = room.name;
        }
    });
}

function removeChatRoomFromList(roomId) {
    const chatRoomListItems = document.querySelectorAll(".list-group-item-action");
    chatRoomListItems.forEach(item => {
        if (item.dataset.id === roomId.toString()) {
            item.remove();
        }
    });
}

function getCurrentRoomId() {
    return document.querySelector("#currentRoomId").value;
}



function handleRoomClick(roomId, userIsMember, groupName) {
   
    if (userIsMember==='True') {
        window.location.href = '/Chat/Room/' + roomId;
    } else {
        // Confirm join group request
        confirmJoinGroup(roomId, groupName);
    }
}

   


let isJoiningGroup = false;

function confirmJoinGroup(groupId, groupName) {
    if (isJoiningGroup) {
        Swal.fire({
            icon: 'warning',
            title: 'Wait...',
            text: 'A join request is already in progress. Please wait.',
            timer: 2000,
            timerProgressBar: true,
            showConfirmButton: false
        });
        return;
    }

    // Check if there are pending requests for the same group
    $.get("/User/CheckPendingJoinRequests", { groupId: groupId })
        .done(function (result) {
            if (result.hasPendingRequest) {
                Swal.fire({
                    icon: 'error',
                    title: 'Oops...',
                    text: 'You already have a pending request for this group.',
                    timer: 2000,
                    timerProgressBar: true,
                    showConfirmButton: false
                });
            } else {
                Swal.fire({
                    icon: 'question',
                    title: 'Join Group',
                    text: 'Do you want to request to join ' + groupName + '?',
                    showCancelButton: true,
                    confirmButtonText: 'Yes',
                    cancelButtonText: 'No'
                }).then((result) => {
                    if (result.isConfirmed) {
                        isJoiningGroup = true;

                        $.post("/User/RequestJoinGroup", { groupId: groupId })
                            .done(function () {
                                Swal.fire({
                                    icon: 'success',
                                    title: 'Success!',
                                    text: 'Join request sent successfully.',
                                    timer: 2000,
                                    timerProgressBar: true,
                                    showConfirmButton: false
                                });
                                isJoiningGroup = false;
                            })
                            .fail(function () {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Oops...',
                                    text: 'Failed to send join request.',
                                    timer: 2000,
                                    timerProgressBar: true,
                                    showConfirmButton: false
                                });
                                isJoiningGroup = false;
                            });
                    }
                });
            }
        })
        .fail(function () {
            Swal.fire({
                icon: 'error',
                title: 'Oops...',
                text: 'Failed to check pending requests.',
                timer: 2000,
                timerProgressBar: true,
                showConfirmButton: false
            });
        });
}


document.getElementById("messageForm").addEventListener("submit", function (event) {
    event.preventDefault();
    const roomId = document.getElementById("currentRoomId").value;
    const messageInput = document.getElementById("messageInput");
    const message = messageInput.value;

    fetch('/Chat/SendMessage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({ roomId: roomId, messageContent: message })
    }).then(response => {
        if (response.ok) {
            connection.invoke("SendMessage", "@User.Identity.Name", message)
                .catch(err => console.error(err.toString()));
            messageInput.value = '';
        }
    });
});




