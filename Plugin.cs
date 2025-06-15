using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using NLog;
using PhoneApp.Domain.Attributes;
using PhoneApp.Domain.DTO;
using PhoneApp.Domain.Interfaces;

namespace EmployeesUsersLoader.Plugin
{
    [Author(Name = "Anatoly")]
    public class Plugin : IPluggable
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private const string ApiUrl = "https://dummyjson.com/users";

        public IEnumerable<DataTransferObject> Run(IEnumerable<DataTransferObject> args)
        {
            _logger.Info("Начинаем загрузку пользователей из API.");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            var loaded = new List<EmployeesDTO>();

            try
            {
                _logger.Info("HTTP GET \"{0}\"", ApiUrl);
                using (var wc = new WebClient())
                {
                    string json = wc.DownloadString(ApiUrl);
                    _logger.Info("Получено {0} байт.", json.Length);

                    JObject root = JObject.Parse(json);
                    JArray users = (JArray)root["users"];
                    _logger.Info("JSON содержит {0} записей в users.", users.Count);

                    foreach (JToken u in users)
                    {
                        try
                        {
                            var dto = new EmployeesDTO();
                            
                            string first = (string)u["firstName"];
                            string last  = (string)u["lastName"];
                            string phone = (string)u["phone"];
                            
                            dto.Name = first + " " + last;
                            dto.AddPhone(phone);

                            loaded.Add(dto);
                        }
                        catch (Exception inner)
                        {
                            _logger.Warn(inner, "Ошибка при обработке пользователя: {0}", u.ToString());
                        }
                    }

                    _logger.Info("Десериализовано и создано {0} DTO.", loaded.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при загрузке пользователей из API: {0}", ex.Message);
            }
            
            var existing = args.Cast<EmployeesDTO>().ToList();
            existing.AddRange(loaded);

            _logger.Info("Всего в списке после загрузки: {0}", existing.Count);
            return existing.Cast<DataTransferObject>();
        }
    }
}
