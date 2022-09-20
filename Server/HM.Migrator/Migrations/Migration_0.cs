namespace HM.Migrator.Migrations;

public class Migration_0 : IMigration
{
    public async ValueTask Execute(Database db, CancellationToken ct)
    {
        await db.NonQuery(ct, @"
CREATE OR REPLACE FUNCTION base36_encode(IN digits bigint, IN min_width int = 0) RETURNS varchar AS $$
DECLARE
    chars char[]; 
    ret varchar; 
    val bigint; 
BEGIN
    chars := ARRAY['0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'];
    val := digits; 
    ret := ''; 
    IF val < 0 THEN 
        val := val * -1; 
    END IF; 
    WHILE val != 0 LOOP 
        ret := chars[(val % 36)+1] || ret; 
        val := val / 36; 
    END LOOP;

    IF min_width > 0 AND char_length(ret) < min_width THEN 
        ret := lpad(ret, min_width, '0'); 
    END IF;

    RETURN ret;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

CREATE SEQUENCE public.users_id_seq
    AS bigint
    START WITH 100000000
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE public.player_id_seq
    AS bigint
    START WITH 100000000
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE TABLE public.users (
    user_id TEXT PRIMARY KEY DEFAULT base36_encode(nextval('public.users_id_seq')),
    salt TEXT NOT NULL,
    token TEXT NOT NULL,
    player_id TEXT NOT NULL DEFAULT base36_encode(nextval('public.player_id_seq'))
);

CREATE TABLE public.game_data (
    key text PRIMARY KEY,
    value text,
    create_time timestamp without time zone,
    update_time timestamp without time zone
);

CREATE OR REPLACE FUNCTION public.user_drop_pid(uid TEXT) RETURNS TEXT
AS $$
DECLARE pid TEXT;
BEGIN
    UPDATE public.users
        SET player_id = base36_encode(nextval('public.player_id_seq'))
        WHERE user_id = uid
        RETURNING player_id INTO pid;
        
        RETURN pid;
END;
$$ LANGUAGE plpgsql
        ");
    }
}