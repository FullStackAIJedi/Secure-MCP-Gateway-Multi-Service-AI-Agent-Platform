import { Link, NavLink } from "react-router-dom";
import styled from "styled-components";
import { useCurrentUserQuery, useLogoutMutation } from "../hooks/useAuth";

const Bar = styled.nav`
  display: flex;
  align-items: center;
  justify-content: space-between;

  color: white;
  padding: 0 24px;
  height: 56px;
`;

const Left = styled.div`
  display: flex;
  gap: 24px;
  align-items: center;
`;

const Right = styled.div`
  display: flex;
  gap: 16px;
  align-items: center;
`;

const StyledLink = styled(NavLink)`
  color: white;
  text-decoration: none;
  font-size: 1rem;
  padding: 8px 0;
  border-bottom: 1px solid transparent;
  transition:
    color 0.2s,
    border-bottom-color 0.2s;

  &:hover {
    color: #f39c12;
    border-bottom-color: white;
  }

  &.active {
    color: #f39c12;
    font-weight: 600;
  }
`;

const Brand = styled.div`
  color: white;
  text-decoration: none;
  font-size: 1.1rem;
  font-weight: 700;
  margin-right: 8px;

  &:hover {
    color: #f35212;
  }
`;

const UserName = styled.span`
  color: white;
  font-size: 0.95rem;
  white-space: nowrap;
`;

const LogoutButton = styled.button`
  background: transparent;
  border: 1px solid #f35212;
  color: #f35212;
  padding: 6px 14px;
  border-radius: 9px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all 0.2s;

  &:hover {
    background: #f35212;
    border-color: #f35212;
    color: #ffffff;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

export function NavBar() {
  const { data: user } = useCurrentUserQuery();
  const logout = useLogoutMutation();

  return (
    <Bar>
      <Left>
        <Brand>MCP Agent</Brand>
        <StyledLink to="/tasks">Tasks</StyledLink>
        <StyledLink to="/documents">Documents</StyledLink>
        <StyledLink to="/chat">Chat</StyledLink>
      </Left>

      <Right>
        {user && (
          <>
            <UserName>{user.email}</UserName>
            <LogoutButton
              onClick={() => logout.mutate()}
              disabled={logout.isPending}
            >
              {logout.isPending ? "Logging out..." : "Logout"}
            </LogoutButton>
          </>
        )}
      </Right>
    </Bar>
  );
}
