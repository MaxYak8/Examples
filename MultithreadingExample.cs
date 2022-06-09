SemaphoreSlim mySemaphoreSlim = new SemaphoreSlim(1, 1);

private async Task RunSessions(long? minId = null, long? maxId = null)
        {  
            var sessions = await downloadSessionRepository.FindSessionsForInterval(minId, maxId);

            while (sessions.Any())
            {
                sessions = sessions.OrderByDescending(q => q.StartId).Take(MaxNumberOrRunningSessionsAtSameTime).ToList();

                var tasks = new List<Task>();

                foreach (var sessionData in sessions)
                {
                    tasks.Add(AddGamesInSession(sessionData));
                }

                await Task.WhenAll(tasks);

                await downloadSessionRepository.DeleteEndedAndNotValidSessions();
                sessions = await downloadSessionRepository.FindSessionsForInterval(minId, maxId);
            }
        }

        private async Task AddGamesInSession(DownloadSession downloadSession)
        {
            var gameResponsesList = new List<GameData>();
            var gameExternalIds = new List<long>();

            for (var gameExternalId = downloadSession.LastSavedId + 1; gameExternalId <= downloadSession.EndId; gameExternalId++)
            {
                gameExternalIds.Add(gameExternalId);

                if (gameExternalId % NumberOfResponseGames == 0 ||
                   gameExternalId == downloadSession.EndId)
                {
                    await mySemaphoreSlim.WaitAsync();

// get multiple request by Ids to other site to get games data
                    var responses = await httpClientService.GetHtmlResponseGames(gameExternalIds);

                    foreach (var data in responses)
                    {
                        if (data.IncidentsData.Incidents != null &&
                           data.StatisticsData.Statistics != null)
                        {
                            gameResponsesList.Add(data);
                        }
                    }

                    await AddNewGamesInfo(gameExternalId, gameResponsesList, downloadSession.StartId, downloadSession.EndId);

                    mySemaphoreSlim.Release();

                    gameResponsesList.Clear();
                    gameExternalIds.Clear();
                }
            }
        }

        private async Task AddNewGamesInfo(long gameExternalId, List<GameData> gameResponsesList, long minExtenalId, long maxExternalId)
        {
            try
            {
                var downloadSession = await downloadSessionRepository.FindFirstByWhereAsync(q => q.StartId == minExtenalId &&
                                                                                            q.EndId == maxExternalId);

                if (downloadSession != null)
                {

// add/insert/update new or existed games in database by EF 
                    await gameService.AddNewInfoGameCollection(gameResponsesList, httpClientService, downloadSession);

                    downloadSession.LastSavedId = gameExternalId;                   
                    downloadSession.LastUpdate = DateTime.UtcNow;

                    await downloadSessionRepository.UpdateAsync(downloadSession);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.StackTrace);
                logger.LogError(e.Message);
            }
            finally
            {
            }
        }
