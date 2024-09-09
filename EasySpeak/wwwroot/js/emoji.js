// emoji.js
document.addEventListener('DOMContentLoaded', () => {
    const button = document.querySelector('#emojiButton');
    const picker = new EmojiButton();

    picker.on('emoji', emoji => {
        document.querySelector('#messageInput').value += emoji;
    });

    button.addEventListener('click', () => {
        picker.togglePicker(button);
    });
});
