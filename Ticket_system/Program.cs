using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static readonly TelegramBotClient Bot = new TelegramBotClient("8043050564:AAE_SHogTfozQFxlJaDhDFoKmm9mJRQTr4I");

    // Состояния
    private enum BotState
    {
        Start,
        ChooseAction,
        SelectClassroom,
        SelectDate,
        WaitForCustomDate,
        SelectBrokenType,
        AddComment,
        EngineerResponse
    }

    private static BotState _state = BotState.Start;
    private static readonly Dictionary<long, Dictionary<string, string>> UserData = new();

    private static readonly List<string> Classrooms = new List<string> { "102" }
        .Concat(Enumerable.Range(201, 19).Select(i => i.ToString()))
        .Concat(Enumerable.Range(301, 19).Select(i => i.ToString()))
        .Concat(Enumerable.Range(401, 19).Select(i => i.ToString()))
        .ToList();


    private static readonly List<string> BrokenTypes = new()
    {
        "Аппаратные неисправности",
        "Программные неисправности",
        "Сетевые проблемы"
    };

    private static readonly Dictionary<long, bool> Engineers = new()
    {
        { 6039644448, false }, // Свободен
        { 933030175, true },   // Занят
        { 1234567890, false }  // Свободен
    };

    private static readonly List<long> AllowedUsers = new() { 1933030175, 1638245198, 6039644448 };

    static async Task Main()
    {
        using var cts = new CancellationTokenSource();

        // Настраиваем обработчик обновлений
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Получать все типы обновлений
        };

        Bot.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandleErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

        Console.WriteLine("Бот запущен. Нажмите любую клавишу для завершения.");
        Console.ReadLine();
        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } text) return;

        var userId = message.From.Id;
        if (!AllowedUsers.Contains(userId))
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "У вас нет доступа к этому боту.", cancellationToken: cancellationToken);
            return;
        }

        switch (_state)
        {
            case BotState.Start:
                _state = BotState.ChooseAction;
                var startKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("Отправить запрос на починку оборудования")
                })
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Привет! Я бот для тикет-системы. Выберите действие:", replyMarkup: startKeyboard, cancellationToken: cancellationToken);
                break;

            case BotState.ChooseAction:
                _state = BotState.SelectClassroom;
                var classroomKeyboard = new ReplyKeyboardMarkup(Classrooms.Select(c => new KeyboardButton(c)).ToArray())
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, выберите аудиторию:", replyMarkup: classroomKeyboard, cancellationToken: cancellationToken);
                break;

            case BotState.SelectClassroom:
                UserData[userId] = new Dictionary<string, string> { { "Classroom", text } };
                _state = BotState.SelectDate;

                var dateKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("Сегодняшняя дата"),
                    new KeyboardButton("Ввести самостоятельно")
                })
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите дату:", replyMarkup: dateKeyboard, cancellationToken: cancellationToken);
                break;

            case BotState.SelectDate:
                if (text == "Сегодняшняя дата")
                {
                    UserData[userId]["Deadline"] = DateTime.Now.ToString("yyyy-MM-dd");
                    _state = BotState.SelectBrokenType;
                }
                else
                {
                    _state = BotState.WaitForCustomDate;
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, введите дату в формате YYYY-MM-DD:", cancellationToken: cancellationToken);
                }
                break;

            case BotState.WaitForCustomDate:
                UserData[userId]["Deadline"] = text;
                _state = BotState.SelectBrokenType;
                break;

            case BotState.SelectBrokenType:
                UserData[userId]["BrokenType"] = text;
                UserData[userId]["Comment"] = "нету";
                _state = BotState.EngineerResponse;

                var ticketInfo = $"Запрос на починку оборудования:\nАудитория: {UserData[userId]["Classroom"]}\nСрок: {UserData[userId]["Deadline"]}\nТип поломки: {UserData[userId]["BrokenType"]}\nКомментарий: {UserData[userId]["Comment"]}";
                foreach (var engineerId in Engineers.Keys.Where(id => !Engineers[id]))
                {
                    var engineerKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton("Да"),
                        new KeyboardButton("Нет")
                    })
                    {
                        ResizeKeyboard = true
                    };
                    await botClient.SendTextMessageAsync(engineerId, ticketInfo + "\nЕсть ли у вас время?", replyMarkup: engineerKeyboard, cancellationToken: cancellationToken);
                }
                break;

            case BotState.EngineerResponse:
                if (text == "Да")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Тикет принят.", cancellationToken: cancellationToken);
                }
                else if (text == "Нет")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Тикет отклонен.", cancellationToken: cancellationToken);
                }
                break;
        }
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}
