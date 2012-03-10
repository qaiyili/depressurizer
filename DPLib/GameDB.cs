﻿/*
Copyright 2011, 2012 Steve Labbe.

This file is part of Depressurizer.

Depressurizer is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Depressurizer is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Depressurizer.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace DPLib {
    public class GameDB {
        public Dictionary<int, GameDBEntry> Games = new Dictionary<int, GameDBEntry>();
        static char[] genreSep = new char[] { ',' };

        public bool Contains( int id ) {
            return Games.ContainsKey( id );
        }

        public bool IsDlc( int id ) {
            return Games.ContainsKey( id ) && Games[id].Type == AppType.DLC;
        }

        public string GetName( int id ) {
            if( Games.ContainsKey( id ) ) {
                return Games[id].Name;
            } else {
                return null;
            }
        }

        public string GetGenre( int id, bool full ) {
            if( Games.ContainsKey( id ) ) {
                string fullString = Games[id].Genre;
                if( string.IsNullOrEmpty( fullString ) ) {
                    return null;
                } else if( full ) {
                    return fullString;
                } else {
                    int index = fullString.IndexOf( ',' );
                    if( index < 0 ) {
                        return fullString;
                    } else {
                        return fullString.Substring( 0, index );
                    }
                }
            } else {
                return null;
            }
        }

        public static XmlDocument FetchAppList() {
            XmlDocument doc = new XmlDocument();
            WebRequest req = WebRequest.Create( @"http://api.steampowered.com/ISteamApps/GetAppList/v0002/?format=xml" );
            using( WebResponse resp = req.GetResponse() ) {
                doc.Load( resp.GetResponseStream() );
            }
            return doc;
        }

        public void IntegrateAppList( XmlDocument doc ) {
            foreach( XmlNode node in doc.SelectNodes( "/applist/apps/app" ) ) {
                int appId;
                if( XmlUtil.TryGetIntFromNode( node["appid"], out appId ) ) {
                    if( Games.ContainsKey( appId ) ) {
                        GameDBEntry g = Games[appId];
                        if( string.IsNullOrEmpty( g.Name ) ) {
                            g.Name = XmlUtil.GetStringFromNode( node["name"], null );
                        }
                    } else {
                        GameDBEntry g = new GameDBEntry();
                        g.Id = appId;
                        g.Name = XmlUtil.GetStringFromNode( node["name"], null );
                        Games.Add( appId, g );
                    }
                }
            }
        }

        public void UpdateAppList() {
            XmlDocument doc = FetchAppList();
            IntegrateAppList( doc );
        }

        public void SaveToXml( string path ) {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.CloseOutput = true;
            XmlWriter writer = XmlWriter.Create( path, settings );
            writer.WriteStartDocument();
            writer.WriteStartElement( "gamelist" );
            foreach( GameDBEntry g in Games.Values ) {
                writer.WriteStartElement( "game" );

                writer.WriteElementString( "id", g.Id.ToString() );
                if( !string.IsNullOrEmpty( g.Name ) ) {
                    writer.WriteElementString( "name", g.Name );
                }
                writer.WriteElementString( "type", g.Type.ToString() );
                if( !string.IsNullOrEmpty( g.Genre ) ) {
                    writer.WriteElementString( "genre", g.Genre );
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }

        public void LoadFromXml( string path ) {
            XmlDocument doc = new XmlDocument();
            doc.Load( path );

            Games.Clear();

            foreach( XmlNode gameNode in doc.SelectNodes( "/gamelist/game" ) ) {
                int id;
                if( !XmlUtil.TryGetIntFromNode( gameNode["id"], out id ) || Games.ContainsKey( id ) ) {
                    continue;
                }
                GameDBEntry g = new GameDBEntry();
                g.Id = id;
                XmlUtil.TryGetStringFromNode( gameNode["name"], out g.Name );
                string typeString;
                if( !XmlUtil.TryGetStringFromNode( gameNode["type"], out typeString ) || !Enum.TryParse<AppType>( typeString, out g.Type ) ) {
                    g.Type = AppType.New;
                }

                g.Genre = XmlUtil.GetStringFromNode( gameNode["genre"], null );

                Games.Add( id, g );
            }
        }
    }
}
