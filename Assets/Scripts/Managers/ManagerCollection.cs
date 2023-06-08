/*
Jonas Wombacher - Research Project Telecooperation
Copyright (C) 2023 Jonas Wombacher

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

// static class used to allow all managers access to the other managers
public static class ManagerCollection
{
    public static AlignmentManager alignmentManager;
    public static CharacterManager characterManager;
    public static FireManager fireManager;
    public static GameManager gameManager;
    public static ObstacleManager obstacleManager;
    public static SaveLoadManager saveLoadManager;
    public static StatusTextManager statusTextManager;
    public static UIManager uiManager;
}
