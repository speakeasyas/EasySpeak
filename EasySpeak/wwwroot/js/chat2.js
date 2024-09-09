
"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub?roomId=" + document.getElementById("currentRoomId").value)
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.start().catch(function (err) {
    return console.error(err.toString());
});
const currentUserId = document.getElementById("currentUserId").value;

// تحديث التمرير عند إضافة رسالة جديدة
function sendMessage() {
    // إرسال البيانات باستخدام fetch أو AJAX
    const roomId = document.getElementById("currentRoomId").value;
    const message = document.getElementById("messageInput").value;
    //const message2 = $('#messageInput').data("emojioneArea").getText();
        fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ roomId: roomId, messageContent: message })
        }).then(response => {
            if (response.ok) {
                document.getElementById("messageInput").value = '';
                scrollToBottom();
            } else {
                console.error('Failed to send message:', response.statusText);
            }
        }).catch(err => console.error('Fetch error:', err));
    }

connection.on("ReceiveFile", function (user, fileName, fileUrl) {
    console.log(user);
    const fileElement = document.createElement("div");
    fileElement.className = "file-message";
    fileElement.innerHTML = `
    <div class="message-header">
            <img src="/images/Profiles/${user.profilePicture}" alt="${user.name}'s Profile Picture" class="profile-picture" />
            <span class="user">${user.name}</span>
        </div>
        <div class="message-body">
            <a href="${fileUrl}" target="_blank">${fileName}</a>
        </div>`;
    document.getElementById("chatMessages").appendChild(fileElement);
    const chatMessages = document.getElementById("chatMessages");
    chatMessages.scrollTop = chatMessages.scrollHeight;
});
function scrollToBottom() {
    var $chatMessages = $('#chatMessages');
    $chatMessages.scrollTop($chatMessages[0].scrollHeight);
}

// استدعاء الدالة للتمرير لأسفل عند تحميل الصفحة
document.addEventListener("DOMContentLoaded", function () {
    scrollToBottom();

    // أضف event listeners للعناصر الموجودة عند تحميل الصفحة
    document.querySelectorAll(".start-private-chat").forEach(item => {
        item.addEventListener("click", startPrivateChat);
    });
});


// إرسال رسالة في المحادثة الخاصة
function sendPrivateMessage() {
    const roomId = document.getElementById("privateRoomId").value;
    const message = document.getElementById("privateMessageInput").value;

    fetch('/Chat/SendPrivateMessage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({ roomId: roomId, messageContent: message })
    }).then(response => {
        if (response.ok) {
            document.getElementById("privateMessageInput").value = '';
            scrollToPrivateBottom();
        } else {
            console.error('Failed to send message:', response.statusText);
        }
    }).catch(err => console.error('Fetch error:', err));
}
connection.on("ReceiveMessage", function (senderName, profilePicture, messageContent) {
    console.log(`${senderName}: ${messageContent}`);
    console.log(profilePicture);
    const messageElement = document.createElement("div");
    messageElement.className = "message";
    messageElement.innerHTML = `
        <div class="message-header">
            <img src="/images/Profiles/${profilePicture}" alt="${senderName}'s Profile Picture" class="profile-picture" />
            <span class="user">${senderName}</span>
        </div>
        <div class="message-body">
            <span>${messageContent}</span>
        </div>`;

    //<strong>${senderName}</strong>: ${messageContent}`;
    document.getElementById("chatMessages").appendChild(messageElement);
    // التمرير إلى أسفل تلقائيًا عند تلقي رسالة جديدة
    // التمرير إلى أسفل تلقائيًا عند تلقي رسالة جديدة
    const chatMessages = document.getElementById("chatMessages");
    if (chatMessages) {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    } else {
        console.error('Element with id "chatMessages" not found');
    }
});
// استقبال الرسائل الخاصة
connection.on("ReceivePrivateMessage", (user, profilePicture, message) => {
    //console.log(user);
    //console.log(profilePicture);
    //console.log(message);
    const msg = document.createElement("div");
    msg.classList.add("message");
    msg.innerHTML = `
     <div class="message-header">
            <img src="/images/Profiles/${profilePicture}" alt="${user}'s Profile Picture" class="profile-picture" />
            <span class="user">${user}</span>
        </div>
        <div class="message-body">
            <span>${message}</span>
        </div>`;
    document.getElementById("privateChatMessages").appendChild(msg);
    scrollToBottom2(document.getElementById("privateChatMessages"));

    // فتح نافذة المحادثة الخاصة إذا كانت مغلقة
    const privateChatWindow = document.getElementById("privateChatWindow");
    if (privateChatWindow.style.display === "none") {
        privateChatWindow.style.display = "block";
        document.getElementById("privateChatUserName").innerText = user;
    }
});

connection.on("UserJoined", function (userId, userName) {
    const userListItem = document.createElement("li");
    userListItem.className = "user-list-item connected";
    userListItem.id = `user-${userId}`;
    userListItem.textContent = userName;
    document.querySelector(".user-list").appendChild(userListItem);
});

connection.on("UserLeft", function (userId) {
    const userListItem = document.getElementById(`user-${userId}`);
    if (userListItem) {
        userListItem.className = "user-list-item disconnected";
        
        document.querySelector(".user-list").removeChild(userListItem);
        
    }
});
window.addEventListener("beforeunload", function (event) {
    fetch('/api/Chat/UserLeaving', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify(connection.connectionId)
    })
        .then(response => {
            if (response.ok) {
                console.log('UserLeaving request sent successfully');
            } else {
                console.log('Error in UserLeaving request:', response.statusText);
            }
        })
        .catch(error => {
            console.error('Error in UserLeaving request:', error);
        });
});
connection.on("UpdateUsers", function (users) {
    // احصل على قائمة المستخدمين في الشريط الجانبي
    const userList = document.querySelector(".user-list");
    userList.innerHTML = ''; // قم بتفريغ القائمة الحالية

    // أضف المستخدمين المتصلين الحاليين إلى القائمة
    users.forEach(function (user) {
        console.log(user);
        const userListItem = document.createElement("li");
        userListItem.className = "user-list-item connected list-group-item d-flex align-items-center";
        userListItem.id = `user-${user.userId}`;
        const userImage = document.createElement("img");
        userImage.src = '/images/Profiles/'+user.user.profilePicture; // تأكد من أن user.user.profileImageUrl يحتوي على عنوان URL للصورة
        userImage.alt = `${user.user.name}'s profile picture`;
        userImage.className = "user-profile-image rounded-circle mr-3"; // إضافة فئة CSS للصورة
        //userListItem.appendChild(userImage);


        let userNameElement;
        if (user.userId === currentUserId) {
            userNameElement = document.createElement("span");
            userNameElement.textContent = user.user.name; // لا تجعل الاسم رابطًا
        } else {
            userNameElement = document.createElement("a");
            userNameElement.href = "#";
            userNameElement.className = "start-private-chat text-dark text-decoration-none";
            userNameElement.dataset.userId = user.userId;
            userNameElement.dataset.userName = user.user.name;
            userNameElement.textContent = user.user.name;
        }
        userListItem.appendChild(userImage);
        userListItem.appendChild(userNameElement);
        userList.appendChild(userListItem);



        //if (user.userId === currentUserId) {
        //    userListItem.textContent = user.user.name; // لا تجعل الاسم رابطًا
        //} else {
        //    /*userListItem.textContent = user.user.name;*/
        //    userListItem.innerHTML = `<a data-user-id="${user.userId}" data-user-name="${user.user.name}" href="#" class="start-private-chat">${user.user.name}</a>`;
        //}
    //    userList.appendChild(userListItem);
    });
    // أضف event listeners للعناصر الجديدة
    document.querySelectorAll(".start-private-chat").forEach(item => {
        item.addEventListener("click", startPrivateChat);
    });
});

function startPrivateChat(event) {
    const userId = event.target.dataset.userId;
    const userName = event.target.dataset.userName;

    // إخطار الخادم ببدء المحادثة الخاصة
    connection.invoke("StartPrivateChat", userId, userName).catch(err => console.error(err.toString()));

    // فتح نافذة المحادثة الخاصة عند المستخدم الحالي
    document.getElementById("privateChatUserName").innerText = userName;
    document.getElementById("privateRoomId").value = userId;
    document.getElementById("privateChatWindow").style.display = "block";

   
}
connection.on("OpenPrivateChat", function (senderId, senderName) {

    // فتح نافذة المحادثة الخاصة عند المستخدم الآخر
    document.getElementById("privateChatUserName").innerText = senderName;
    document.getElementById("privateRoomId").value = senderId;
    document.getElementById("privateChatWindow").style.display = "block";

});

connection.on("LoadPrivateMessages", function (messages) {
    

    const privateChatMessages = document.getElementById("privateChatMessages");
    privateChatMessages.innerHTML = ''; // تفريغ الرسائل الحالية

    messages.forEach(function (message) {
        const msg = document.createElement("div");
        msg.classList.add("message");
        console.log(message);
        if (message.content !== "") {
            msg.innerHTML =
                `
     <div class="message-header">
            <img src="/images/Profiles/${message.spicture}" alt="${message.sname}'s Profile Picture" class="profile-picture" />
            <span class="user">${message.sname}</span>
        </div>
        <div class="message-body">
            
            <span>${message.content}</span>
        </div>`;
        }
        else {
            msg.innerHTML =
                `
     <div class="message-header">
            <img src="/images/Profiles/${message.spicture}" alt="${message.sname}'s Profile Picture" class="profile-picture" />
            <span class="user">${message.sname}</span>
        </div>
        <div class="message-body">
            
          <span><a href="${message.filePath}" target="_blank" >${message.filePath} </a></span>
        </div>`;
        }


        //    `<span class="user">${message.sname}:</span> <span>${message.content}</span>`;
        privateChatMessages.appendChild(msg);
    });
    privateChatMessages.scrollTop = privateChatMessages.scrollHeight;

});
//document.getElementById("emojiButton").addEventListener("click", function () {
//    const messageInput = document.getElementById("messageInput");
//    messageInput.value += '😊';
//});
document.getElementById("messageForm").addEventListener("submit", function (event) {
    var messageContent = $("#messageInput").val().trim();
    if (messageContent === "") {
        event.preventDefault();
    }
    else {
        event.preventDefault(); // منع النموذج من الإرسال الافتراضي
        sendMessage(); // استدعاء دالة إرسال الرسالة
        const messageInput = document.getElementById("messageInput");
        messageInput.textContent = '';
        //$('#messageInput').data("emojioneArea").setText('');
    }

});

document.getElementById("messageInput").addEventListener("keypress", function (event) {
    if (event.key === 'Enter') { // التحقق إذا كانت الضغطة على زر Enter

        var messageContent = $(this).val().trim();
        if (messageContent === "") {
            alert("لا يمكن إرسال رسالة فارغة");
            event.preventDefault();
        }
        else {

            event.preventDefault(); // منع السلوك الافتراضي لزر Enter
            sendMessage(); // استدعاء دالة إرسال الرسالة
            const messageInput = document.getElementById("messageInput");
            messageInput.textContent = '';
            //    $('#messageInput').data("emojioneArea").setText('');
        }
    }
});



document.getElementById("privateMessageForm").addEventListener("submit", (event) => {
    event.preventDefault();
    const recipientId = document.getElementById("privateRoomId").value;
    const message = document.getElementById("privateMessageInput").value;
    connection.invoke("SendPrivateMessage", recipientId, message).catch(err => console.error(err.toString()));
    document.getElementById("privateMessageInput").value = "";
    scrollToBottom2(document.getElementById("privateChatMessages"));
});

document.querySelectorAll(".user-list-item").forEach(item => {
    item.addEventListener("click", (event) => {
        const userId = event.target.dataset.userId;
        const userName = event.target.innerText;
        document.getElementById("privateChatUserName").innerText = userName;
        document.getElementById("privateRoomId").value = userId;
        document.getElementById("privateChatWindow").style.display = "block";
    });
});

document.getElementById("closePrivateChat").addEventListener("click", () => {
    document.getElementById("privateChatWindow").style.display = "none";
});
function scrollToBottom2(element) {
    element.scrollTop = element.scrollHeight;
}


// دالة لإرسال ملف في المحادثة الخاصة
function sendPrivateFile() {
    const recipientId = document.getElementById("privateRoomId").value;
    const fileInput = document.getElementById("privateFileInput");

    if (fileInput.files.length > 0) {
        const formData = new FormData();
        formData.append("file", fileInput.files[0]);
        formData.append("recipientId", recipientId);

        fetch('/Chat/SendPrivateFile', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: formData
        }).then(response => {
            if (response.ok) {
                fileInput.value = ''; // إعادة تعيين المدخل
                console.log('File sent successfully');
            } else {
                console.error('Failed to send file:', response.statusText);
            }
        }).catch(err => console.error('Fetch error:', err));
    }
}

// استقبال الملفات الخاصة
connection.on("ReceivePrivateFile", function (senderId, profilePicture, fileName, fileUrl) {
    const privateChatMessages = document.getElementById("privateChatMessages");
    const fileElement = document.createElement("div");
    fileElement.className = "file-message";
    fileElement.innerHTML = `
        <div class="message-header">
            <img src="/images/Profiles/${profilePicture}" alt="${senderId}'s Profile Picture" class="profile-picture" />
            <span class="user">${senderId}</span>
        </div>
        <div class="message-body">
            <a href="${fileUrl}" target="_blank">${fileName}</a>
        </div>`;
    privateChatMessages.appendChild(fileElement);
    privateChatMessages.scrollTop = privateChatMessages.scrollHeight;
});

// ربط حدث إرسال الملف
document.getElementById("pfileButton").addEventListener("click", function () {
    const fileInput = document.createElement("input");
    fileInput.type = "file";
    fileInput.style.display = "none";
    fileInput.id = "privateFileInput";
    document.body.appendChild(fileInput);

    fileInput.addEventListener("change", function () {
        sendPrivateFile();
        document.body.removeChild(fileInput);
    });

    fileInput.click();
});
//document.getElementById("leaveRoomButton").addEventListener("click", function () {
//    const roomId = document.getElementById("currentRoomId").value;
//    console.log("LeaveRoom button clicked. RoomId:", roomId);

//    connection.invoke("LeaveRoom", roomId).then(function () {
//        console.log("LeaveRoom invoked successfully.");
//        window.location.href = "/Chat/Index"; // إعادة توجيه المستخدم إلى صفحة أخرى بعد خروجه
//    }).catch(function (err) {
//        console.error("Error invoking LeaveRoom:", err.toString());
//        alert("An error occurred while trying to leave the room.");
//    });
//});

connection.on("UserExit", function (userId) {
    Swal.fire({
        icon: 'info',
        title: 'Notification',
        text: userId+' Left The Room',
    });

    //const userListItem = document.getElementById(`user-${userId}`);
    //if (userListItem) {
    //    userListItem.className = "user-list-item disconnected";
    //    userListItem.innerText = `${userId} has left the group`;
    //}
});

document.getElementById("leaveRoomButton").addEventListener("click", function () {
    const roomId = document.getElementById("currentRoomId1").value;
    console.log("LeaveRoom button clicked. RoomId:", roomId);

    fetch('/Chat/LeaveRoom', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify(roomId) // إرسال roomId كـ string بدلاً من كائن
    }).then(response => {
        if (response.ok) {
            console.log("LeaveRoom invoked successfully.");
            window.location.href = "/User/Index"; // إعادة توجيه المستخدم إلى صفحة أخرى بعد خروجه
        } else {
            console.error("Error invoking LeaveRoom:", response.statusText);
            alert("An error occurred while trying to leave the room.");
        }
    }).catch(function (err) {
        console.error("Error invoking LeaveRoom:", err.toString());
        alert("An error occurred while trying to leave the room.");
    });
});

